using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Travio.Core.Domain.Entities.Destinations;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.DTOs.GenericResponse;
using Travio.Infrastructure.Repositories;
using Travio.UnitTests.Fixtures;

namespace Travio.UnitTests.Repositories;

public class GenericRepositoryAndUnitOfWorkTests
{
    [Fact]
    public async Task GenericRepository_Add_Query_Update_Delete_ShouldPersistCorrectly()
    {
        using var db = new SqliteTestDatabase();
        var repo = new GenericRepository<Continent>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var continent = new Continent
        {
            ContinentID = 1,
            Name = "Europe"
        };

        await uow.ExecuteAsync(async () =>
        {
            await repo.AddAsync(continent);
        });

        var fetched = await repo.GetQueryable()
            .Where(c => c.ContinentID == 1)
            .Select(c => new { c.ContinentID, c.Name })
            .FirstOrDefaultAsync();

        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Europe");

        // Update
        continent.Name = "Updated Europe";
        await uow.ExecuteAsync(() =>
        {
            repo.Update(continent);
            return Task.CompletedTask;
        });

        var updatedName = await repo.GetQueryable()
            .Where(c => c.ContinentID == 1)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();
        updatedName.Should().Be("Updated Europe");

        // Delete
        await uow.ExecuteAsync(() =>
        {
            repo.Delete(continent);
            return Task.CompletedTask;
        });

        var exists = await repo.GetQueryable().AnyAsync(c => c.ContinentID == 1);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GenericRepository_SaveInclude_ShouldModifyOnlySpecifiedColumns_AndPreservePrimaryKeyAndConcurrencyToken()
    {
        using var db = new SqliteTestDatabase();
        var repo = new GenericRepository<HotelBooking>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var bookingId = Guid.NewGuid();
        var originalBooking = new HotelBooking
        {
            Id = bookingId,
            UserId = "user-123",
            HotelId = 500,
            HotelName = "Original Grand Hotel",
            CheckIn = new DateOnly(2026, 11, 1),
            CheckOut = new DateOnly(2026, 11, 5),
            TotalPrice = 450.00m,
            Currency = "USD",
            BookingStatus = HotelBookingStatus.PendingPayment,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        await uow.ExecuteAsync(async () =>
        {
            await repo.AddAsync(originalBooking);
        });

        db.Context.ChangeTracker.Clear();

        // Create a detached stub with modified HotelName (which should NOT be saved)
        // and modified BookingStatus + HotelbedsReference (which SHOULD be saved)
        var partialUpdate = new HotelBooking
        {
            Id = bookingId,
            HotelName = "HACKED NAME SHOULD NOT SAVE",
            TotalPrice = 99999m, // Should NOT be saved
            BookingStatus = HotelBookingStatus.Confirmed,
            HotelbedsReference = "HB-REF-999",
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        repo.SaveInclude(partialUpdate, nameof(HotelBooking.BookingStatus), nameof(HotelBooking.HotelbedsReference));

        var entry = db.Context.Entry(partialUpdate);
        entry.Property(x => x.BookingStatus).IsModified.Should().BeTrue();
        entry.Property(x => x.HotelbedsReference).IsModified.Should().BeTrue();
        entry.Property(x => x.HotelName).IsModified.Should().BeFalse();
        entry.Property(x => x.TotalPrice).IsModified.Should().BeFalse();
        entry.Property(x => x.Id).IsModified.Should().BeFalse();

        await uow.SaveChangesAsync();

        await using var verifyCtx = db.CreateNewContext();
        var reloaded = await verifyCtx.HotelBookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        reloaded.BookingStatus.Should().Be(HotelBookingStatus.Confirmed);
        reloaded.HotelbedsReference.Should().Be("HB-REF-999");
        reloaded.HotelName.Should().Be("Original Grand Hotel");
        reloaded.TotalPrice.Should().Be(450.00m);
    }

    [Fact]
    public async Task UnitOfWork_WhenServiceResponseFailsBusinessRule_ShouldAutomaticallyRollbackTransaction()
    {
        using var db = new SqliteTestDatabase();
        var repo = new GenericRepository<Continent>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var response = await uow.ExecuteAsync(async () =>
        {
            await repo.AddAsync(new Continent
            {
                ContinentID = 99,
                Name = "Atlantis"
            });
            await db.Context.SaveChangesAsync();

            // Returning a failed ServiceResponse triggers IServiceResult.BusinessRuleFailed == true
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "Business rule validation failed"
            };
        });

        response.Success.Should().BeFalse();

        await using var verifyCtx = db.CreateNewContext();
        var exists = await verifyCtx.Continents.AnyAsync(c => c.ContinentID == 99);
        exists.Should().BeFalse("UnitOfWork must rollback the transaction when IServiceResult.BusinessRuleFailed is true");
    }

    [Fact]
    public async Task UnitOfWork_WhenExceptionIsThrown_ShouldRollbackAllChanges()
    {
        using var db = new SqliteTestDatabase();
        var repo = new GenericRepository<Continent>(db.Context);
        var uow = new UnitOfWork(db.Context);

        Func<Task> act = async () =>
        {
            await uow.ExecuteAsync(async () =>
            {
                await repo.AddAsync(new Continent
                {
                    ContinentID = 77,
                    Name = "Lemuria"
                });
                await db.Context.SaveChangesAsync();

                throw new InvalidOperationException("Simulated crash mid-transaction");
            });
        };

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var verifyCtx = db.CreateNewContext();
        var exists = await verifyCtx.Continents.AnyAsync(c => c.ContinentID == 77);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task UnitOfWork_NestedSavepoints_ShouldRollbackInnerFailedScope_WhileCommittingOuterScope()
    {
        using var db = new SqliteTestDatabase();
        var repo = new GenericRepository<Continent>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var outerResult = await uow.ExecuteAsync(async () =>
        {
            // Outer scope adds Asia
            await repo.AddAsync(new Continent
            {
                ContinentID = 10,
                Name = "Asia"
            });
            await db.Context.SaveChangesAsync();

            // Nested inner scope adds InvalidContinent and returns a failed ServiceResponse
            var innerResult = await uow.ExecuteAsync(async () =>
            {
                await repo.AddAsync(new Continent
                {
                    ContinentID = 20,
                    Name = "RolledBackInnerContinent"
                });
                await db.Context.SaveChangesAsync();

                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Inner savepoint should rollback"
                };
            });

            innerResult.Success.Should().BeFalse();

            // Detach inner entity from EF ChangeTracker so outer SaveChanges doesn't re-insert it
            db.Context.ChangeTracker.Clear();

            return new ServiceResponse<string>
            {
                Success = true,
                Data = "Outer succeeded"
            };
        });

        outerResult.Success.Should().BeTrue();

        await using var verifyCtx = db.CreateNewContext();
        var asiaExists = await verifyCtx.Continents.AnyAsync(c => c.ContinentID == 10);
        var innerExists = await verifyCtx.Continents.AnyAsync(c => c.ContinentID == 20);

        asiaExists.Should().BeTrue("Outer scope succeeded and committed");
        innerExists.Should().BeFalse("Inner scope was rolled back to its savepoint SP_Depth1");
    }
}
