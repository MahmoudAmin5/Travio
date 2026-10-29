using System.Text.Json;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Stripe;
using Travio.Core.Contracts.Services.CurruncyExchange;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.GeocodingService;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.Contracts.Services.Payment;
using Travio.Core.Domain.Entities.Duffel;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Enums.Booking;
using Travio.Core.DTOs.DuffelFlightsDTOs;
using Travio.Core.DTOs.DuffelFlightsDTOs.Requests;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.HotelbedsDTOs.Responses;
using Travio.Core.Services.Hotelbeds;
using Travio.Core.Services.Payment;
using Travio.Core.Setting;
using Travio.Infrastructure.Repositories;
using Travio.UnitTests.Fixtures;

namespace Travio.UnitTests.Services;

public class PaymentAndBookingServiceTests
{
    [Fact]
    public async Task StripeWebhookService_HotelPaymentSuccess_ShouldAcquireLock_FulfillBooking_AndEnqueueEmailJob()
    {
        using var db = new SqliteTestDatabase();
        var flightRepo = new GenericRepository<FlightBooking>(db.Context);
        var hotelRepo = new GenericRepository<HotelBooking>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var bookingId = Guid.NewGuid();
        var intentId = "pi_hotel_success_001";

        await uow.ExecuteAsync(async () =>
        {
            await hotelRepo.AddAsync(new HotelBooking
            {
                Id = bookingId,
                UserId = "user-hotel-1",
                HotelId = 101,
                HotelName = "Nile Ritz-Carlton",
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                TotalPrice = 850.00m,
                Currency = "USD",
                StripePaymentIntentId = intentId,
                BookingStatus = HotelBookingStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow
            });
        });

        var flightServiceMock = new Mock<IDuffelFlightBookingService>();
        var hotelServiceMock = new Mock<IHotelbedsService>();
        var paymentGatewayMock = new Mock<IPaymentGatewayService>();
        var backgroundJobMock = new Mock<IBackgroundJobClient>();

        hotelServiceMock
            .Setup(s => s.FulfillBookingFromWebhookAsync(bookingId, It.IsAny<CancellationToken>()))
            .Callback(async () =>
            {
                await hotelRepo.GetQueryable()
                    .Where(b => b.Id == bookingId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.BookingStatus, HotelBookingStatus.Confirmed)
                        .SetProperty(b => b.HotelbedsReference, "HB-CONF-777"));
            })
            .ReturnsAsync(new ServiceResponse<HotelBookingResponseDto>
            {
                Success = true,
                Data = new HotelBookingResponseDto { BookingReference = "HB-CONF-777" }
            });

        var webhookService = new StripeWebhookService(
            flightRepo,
            hotelRepo,
            flightServiceMock.Object,
            hotelServiceMock.Object,
            paymentGatewayMock.Object,
            NullLogger<StripeWebhookService>.Instance,
            backgroundJobMock.Object);

        var paymentIntent = new PaymentIntent
        {
            Id = intentId,
            Metadata = new Dictionary<string, string>
            {
                ["BookingType"] = "Hotel",
                ["BookingId"] = bookingId.ToString()
            }
        };

        var result = await webhookService.ProcessPaymentSuccessAsync(paymentIntent);

        result.Should().BeTrue();
        hotelServiceMock.Verify(s => s.FulfillBookingFromWebhookAsync(bookingId, It.IsAny<CancellationToken>()), Times.Once);
        backgroundJobMock.Verify(
            x => x.Create(
                It.Is<Job>(job => job.Method.Name == "SendHotelTicketAsync" && (Guid)job.Args[0] == bookingId),
                It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task StripeWebhookService_ConcurrentDuplicateHotelWebhook_ShouldBeIgnoredIdempotently()
    {
        using var db = new SqliteTestDatabase();
        var flightRepo = new GenericRepository<FlightBooking>(db.Context);
        var hotelRepo = new GenericRepository<HotelBooking>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var bookingId = Guid.NewGuid();
        var intentId = "pi_hotel_dup_002";

        // Simulate a booking that is already actively ProcessingWebhook (UpdatedAt = UtcNow)
        await uow.ExecuteAsync(async () =>
        {
            await hotelRepo.AddAsync(new HotelBooking
            {
                Id = bookingId,
                UserId = "user-hotel-2",
                HotelId = 102,
                HotelName = "Four Seasons Cairo",
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)),
                TotalPrice = 1200.00m,
                Currency = "USD",
                StripePaymentIntentId = intentId,
                BookingStatus = HotelBookingStatus.ProcessingWebhook,
                UpdatedAt = DateTime.UtcNow
            });
        });

        var flightServiceMock = new Mock<IDuffelFlightBookingService>();
        var hotelServiceMock = new Mock<IHotelbedsService>();
        var paymentGatewayMock = new Mock<IPaymentGatewayService>();
        var backgroundJobMock = new Mock<IBackgroundJobClient>();

        var webhookService = new StripeWebhookService(
            flightRepo,
            hotelRepo,
            flightServiceMock.Object,
            hotelServiceMock.Object,
            paymentGatewayMock.Object,
            NullLogger<StripeWebhookService>.Instance,
            backgroundJobMock.Object);

        var paymentIntent = new PaymentIntent
        {
            Id = intentId,
            Metadata = new Dictionary<string, string>
            {
                ["BookingType"] = "Hotel",
                ["BookingId"] = bookingId.ToString()
            }
        };

        var result = await webhookService.ProcessPaymentSuccessAsync(paymentIntent);

        result.Should().BeTrue();
        hotelServiceMock.Verify(s => s.FulfillBookingFromWebhookAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StripeWebhookService_StaleProcessingWebhookLease_ShouldRecoverAndCompleteFulfillment()
    {
        using var db = new SqliteTestDatabase();
        var flightRepo = new GenericRepository<FlightBooking>(db.Context);
        var hotelRepo = new GenericRepository<HotelBooking>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var bookingId = Guid.NewGuid();
        var intentId = "pi_hotel_stale_003";

        // Simulate a worker crash 5 minutes ago while in ProcessingWebhook state (> 3 min lease timeout)
        await uow.ExecuteAsync(async () =>
        {
            await hotelRepo.AddAsync(new HotelBooking
            {
                Id = bookingId,
                UserId = "user-hotel-3",
                HotelId = 103,
                HotelName = "Marriott Mena House",
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)),
                TotalPrice = 950.00m,
                Currency = "USD",
                StripePaymentIntentId = intentId,
                BookingStatus = HotelBookingStatus.ProcessingWebhook,
                UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
            });
        });

        var flightServiceMock = new Mock<IDuffelFlightBookingService>();
        var hotelServiceMock = new Mock<IHotelbedsService>();
        var paymentGatewayMock = new Mock<IPaymentGatewayService>();
        var backgroundJobMock = new Mock<IBackgroundJobClient>();

        hotelServiceMock
            .Setup(s => s.FulfillBookingFromWebhookAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResponse<HotelBookingResponseDto>
            {
                Success = true,
                Data = new HotelBookingResponseDto { BookingReference = "HB-REC-333" }
            });

        var webhookService = new StripeWebhookService(
            flightRepo,
            hotelRepo,
            flightServiceMock.Object,
            hotelServiceMock.Object,
            paymentGatewayMock.Object,
            NullLogger<StripeWebhookService>.Instance,
            backgroundJobMock.Object);

        var paymentIntent = new PaymentIntent
        {
            Id = intentId,
            Metadata = new Dictionary<string, string>
            {
                ["BookingType"] = "Hotel",
                ["BookingId"] = bookingId.ToString()
            }
        };

        var result = await webhookService.ProcessPaymentSuccessAsync(paymentIntent);

        result.Should().BeTrue();
        hotelServiceMock.Verify(s => s.FulfillBookingFromWebhookAsync(bookingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StripeWebhookService_WhenHotelSupplierFails_ShouldTriggerIdempotentRefundAndMarkBookingRefundIssued()
    {
        using var db = new SqliteTestDatabase();
        var flightRepo = new GenericRepository<FlightBooking>(db.Context);
        var hotelRepo = new GenericRepository<HotelBooking>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var bookingId = Guid.NewGuid();
        var intentId = "pi_hotel_fail_refund_004";

        await uow.ExecuteAsync(async () =>
        {
            await hotelRepo.AddAsync(new HotelBooking
            {
                Id = bookingId,
                UserId = "user-hotel-4",
                HotelId = 104,
                HotelName = "Sofitel Legend Old Cataract",
                CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)),
                CheckOut = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(18)),
                TotalPrice = 1500.00m,
                Currency = "USD",
                StripePaymentIntentId = intentId,
                BookingStatus = HotelBookingStatus.PendingPayment
            });
        });

        var flightServiceMock = new Mock<IDuffelFlightBookingService>();
        var hotelServiceMock = new Mock<IHotelbedsService>();
        var paymentGatewayMock = new Mock<IPaymentGatewayService>();
        var backgroundJobMock = new Mock<IBackgroundJobClient>();

        // Supplier fails to confirm room
        hotelServiceMock
            .Setup(s => s.FulfillBookingFromWebhookAsync(bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServiceResponse<HotelBookingResponseDto>
            {
                Success = false,
                Message = "Room sold out on supplier"
            });

        paymentGatewayMock
            .Setup(p => p.RefundPaymentAsync(intentId, "requested_by_customer", bookingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult("re_test_123", true));

        var webhookService = new StripeWebhookService(
            flightRepo,
            hotelRepo,
            flightServiceMock.Object,
            hotelServiceMock.Object,
            paymentGatewayMock.Object,
            NullLogger<StripeWebhookService>.Instance,
            backgroundJobMock.Object);

        var paymentIntent = new PaymentIntent
        {
            Id = intentId,
            Metadata = new Dictionary<string, string>
            {
                ["BookingType"] = "Hotel",
                ["BookingId"] = bookingId.ToString()
            }
        };

        var result = await webhookService.ProcessPaymentSuccessAsync(paymentIntent);

        result.Should().BeTrue();
        paymentGatewayMock.Verify(
            p => p.RefundPaymentAsync(intentId, "requested_by_customer", bookingId, It.IsAny<CancellationToken>()),
            Times.Once);

        await using var verifyCtx = db.CreateNewContext();
        var updatedBooking = await verifyCtx.HotelBookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        updatedBooking.BookingStatus.Should().Be(HotelBookingStatus.RefundIssued);
        updatedBooking.FailureReason.Should().Be("Room sold out on supplier");
    }

    [Fact]
    public async Task StripeWebhookService_FlightPaymentSuccess_ShouldConfirmOnDuffel_UpdatePnr_AndEnqueueEmailJob()
    {
        using var db = new SqliteTestDatabase();
        var flightRepo = new GenericRepository<FlightBooking>(db.Context);
        var hotelRepo = new GenericRepository<HotelBooking>(db.Context);
        var uow = new UnitOfWork(db.Context);

        var bookingId = Guid.NewGuid();
        var intentId = "pi_flight_success_005";
        var passengers = new List<PassengerDetailsDto>
        {
            new()
            {
                GivenName = "Omar",
                FamilyName = "Hassan",
                Email = "omar@example.com",
                PhoneNumber = "+201000000000",
                BornOn = "1995-05-15",
                Gender = "m",
                Title = "mr"
            }
        };

        await uow.ExecuteAsync(async () =>
        {
            await flightRepo.AddAsync(new FlightBooking
            {
                Id = bookingId,
                UserId = "user-flight-1",
                OfferId = "off_duffel_123",
                TotalPrice = 320.50m,
                Currency = "USD",
                StripePaymentIntentId = intentId,
                PassengersJson = JsonSerializer.Serialize(passengers),
                BookingStatus = FlightBookingStatus.PendingPayment,
                PNR = null
            });
        });

        var flightServiceMock = new Mock<IDuffelFlightBookingService>();
        var hotelServiceMock = new Mock<IHotelbedsService>();
        var paymentGatewayMock = new Mock<IPaymentGatewayService>();
        var backgroundJobMock = new Mock<IBackgroundJobClient>();

        flightServiceMock
            .Setup(f => f.CreateOrderAsync(It.Is<FlightOrderRequestDto>(r => r.OfferId == "off_duffel_123" && r.TotalAmount == 320.50m)))
            .ReturnsAsync(new ServiceResponse<FlightOrderResponseDto>
            {
                Success = true,
                Data = new FlightOrderResponseDto
                {
                    PNR = "PNR987"
                }
            });

        var webhookService = new StripeWebhookService(
            flightRepo,
            hotelRepo,
            flightServiceMock.Object,
            hotelServiceMock.Object,
            paymentGatewayMock.Object,
            NullLogger<StripeWebhookService>.Instance,
            backgroundJobMock.Object);

        var paymentIntent = new PaymentIntent
        {
            Id = intentId,
            Metadata = new Dictionary<string, string>
            {
                ["BookingType"] = "Flight",
                ["BookingId"] = bookingId.ToString()
            }
        };

        var result = await webhookService.ProcessPaymentSuccessAsync(paymentIntent);

        result.Should().BeTrue();

        await using var verifyCtx = db.CreateNewContext();
        var updatedFlight = await verifyCtx.FlightBookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        updatedFlight.BookingStatus.Should().Be(FlightBookingStatus.Confirmed);
        updatedFlight.PNR.Should().Be("PNR987");

        backgroundJobMock.Verify(
            x => x.Create(
                It.Is<Job>(job => job.Method.Name == "SendFlightTicketAsync" && (Guid)job.Args[0] == bookingId),
                It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task HotelbedsService_SearchDestinationsAndGetUserBookings_ShouldProjectDirectlyInSql()
    {
        using var db = new SqliteTestDatabase();
        var bookingRepo = new GenericRepository<HotelBooking>(db.Context);
        var destinationRepo = new GenericRepository<HotelDestination>(db.Context);
        var facilityRepo = new GenericRepository<HotelFacility>(db.Context);
        var uow = new UnitOfWork(db.Context);

        await uow.ExecuteAsync(async () =>
        {
            await destinationRepo.AddRangeAsync(new[]
            {
                new HotelDestination { Code = "CAI", Name = "Cairo", CountryCode = "EG" },
                new HotelDestination { Code = "LXR", Name = "Luxor", CountryCode = "EG" },
                new HotelDestination { Code = "PAR", Name = "Paris", CountryCode = "FR" }
            });

            await bookingRepo.AddRangeAsync(new[]
            {
                new HotelBooking
                {
                    Id = Guid.NewGuid(),
                    UserId = "user-A",
                    HotelId = 1,
                    HotelName = "Cairo Marriott",
                    CheckIn = new DateOnly(2026, 12, 1),
                    CheckOut = new DateOnly(2026, 12, 5),
                    TotalPrice = 600m,
                    Currency = "USD",
                    HotelbedsReference = "HB-111",
                    BookingStatus = HotelBookingStatus.Confirmed,
                    RoomCount = 1,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new HotelBooking
                {
                    Id = Guid.NewGuid(),
                    UserId = "user-A",
                    HotelId = 2,
                    HotelName = "Luxor Hilton",
                    CheckIn = new DateOnly(2026, 12, 10),
                    CheckOut = new DateOnly(2026, 12, 15),
                    TotalPrice = 900m,
                    Currency = "USD",
                    HotelbedsReference = "HB-222",
                    BookingStatus = HotelBookingStatus.Confirmed,
                    RoomCount = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new HotelBooking
                {
                    Id = Guid.NewGuid(),
                    UserId = "user-B",
                    HotelId = 3,
                    HotelName = "Other User Hotel",
                    CheckIn = new DateOnly(2026, 12, 1),
                    CheckOut = new DateOnly(2026, 12, 3),
                    TotalPrice = 300m,
                    Currency = "USD",
                    BookingStatus = HotelBookingStatus.Confirmed,
                    RoomCount = 1,
                    CreatedAt = DateTime.UtcNow
                }
            });
        });

        var service = new HotelbedsService(
            new HttpClient(),
            bookingRepo,
            destinationRepo,
            facilityRepo,
            uow,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new HotelbedsSettings
            {
                ApiKey = "test",
                SharedSecret = "test",
                BaseUrl = "https://api.test.hotelbeds.com"
            }),
            Mock.Of<ICurrencyExchangeService>(),
            Mock.Of<IPaymentGatewayService>(),
            NullLogger<HotelbedsService>.Instance,
            Mock.Of<IGeocodingService>());

        var destResult = await service.SearchDestinationsAsync("Cai", CancellationToken.None);
        destResult.Success.Should().BeTrue();
        destResult.Data.Should().ContainSingle(d => d.Code == "CAI" && d.Name == "Cairo");

        var userBookingsResult = await service.GetUserBookingsAsync("user-A", CancellationToken.None);
        userBookingsResult.Success.Should().BeTrue();
        userBookingsResult.Data.Should().HaveCount(2);
        userBookingsResult.Data![0].HotelName.Should().Be("Luxor Hilton", "Most recent booking should be ordered first");
        userBookingsResult.Data![1].HotelName.Should().Be("Cairo Marriott");
    }
}
