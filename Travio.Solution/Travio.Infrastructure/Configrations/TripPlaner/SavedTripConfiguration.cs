using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.TripPlaner;

namespace Travio.Infrastructure.Configrations.TripPlaner;

public class SavedTripConfiguration : IEntityTypeConfiguration<SavedTrip>
{
    public void Configure(EntityTypeBuilder<SavedTrip> builder)
    {
        builder.ToTable("SavedTrips");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.DestinationName)
            .HasMaxLength(256);

        builder.Property(x => x.TotalDays)
            .IsRequired();

        builder.Property(x => x.IsFavorite)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.SavedTrips)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChatSession)
            .WithOne(x => x.SavedTrip)
            .HasForeignKey<SavedTrip>(x => x.ChatSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Days)
            .WithOne(x => x.SavedTrip)
            .HasForeignKey(x => x.SavedTripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Hotels)
            .WithOne(x => x.SavedTrip)
            .HasForeignKey(x => x.SavedTripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsFavorite });
    }
}

public class SavedTripDayConfiguration : IEntityTypeConfiguration<SavedTripDay>
{
    public void Configure(EntityTypeBuilder<SavedTripDay> builder)
    {
        builder.ToTable("SavedTripDays");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayNumber)
            .IsRequired();

        builder.Property(x => x.Theme)
            .HasMaxLength(256);

        builder.HasMany(x => x.Activities)
            .WithOne(x => x.SavedTripDay)
            .HasForeignKey(x => x.SavedTripDayId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SavedTripActivityConfiguration : IEntityTypeConfiguration<SavedTripActivity>
{
    public void Configure(EntityTypeBuilder<SavedTripActivity> builder)
    {
        builder.ToTable("SavedTripActivities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActivityType)
            .HasMaxLength(100);

        builder.Property(x => x.PlaceName)
            .HasMaxLength(256);

        builder.Property(x => x.SuggestedTime)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.FeaturedImage)
            .HasMaxLength(1000);
    }
}

public class SavedTripHotelConfiguration : IEntityTypeConfiguration<SavedTripHotel>
{
    public void Configure(EntityTypeBuilder<SavedTripHotel> builder)
    {
        builder.ToTable("SavedTripHotels");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Address)
            .HasMaxLength(500);

        builder.Property(x => x.Link)
            .HasMaxLength(1000);

        builder.Property(x => x.FeaturedImage)
            .HasMaxLength(1000);
    }
}
