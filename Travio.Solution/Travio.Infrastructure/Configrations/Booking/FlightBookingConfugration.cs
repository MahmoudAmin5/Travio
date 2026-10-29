using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.Duffel;

namespace Travio.Infrastructure.Configrations.Booking
{
    public class FlightBookingConfiguration : IEntityTypeConfiguration<FlightBooking>
    {
        public void Configure(EntityTypeBuilder<FlightBooking> builder)
        {
            builder.ToTable("FlightBookings", "Booking");
            builder.HasKey(e => e.Id);

            // Bounded column lengths to prevent nvarchar(max) table scans
            builder.Property(e => e.UserId)
                   .IsRequired()
                   .HasMaxLength(450);

            builder.Property(e => e.OfferId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(e => e.PNR)
                   .HasMaxLength(50);

            builder.Property(e => e.TotalPrice)
                   .HasPrecision(18, 2);

            builder.Property(e => e.Currency)
                   .IsRequired()
                   .HasMaxLength(3);

            builder.Property(e => e.StripePaymentIntentId)
                   .HasMaxLength(100);

            builder.Property(e => e.FailureReason)
                   .HasMaxLength(1000);

            builder.Property(e => e.BookingStatus)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            builder.Property(e => e.RowVersion)
                   .IsRowVersion();

            // 1. Filtered Unique Index on PNR (only enforced when PNR is assigned upon confirmation)
            builder.HasIndex(b => b.PNR)
                   .IsUnique()
                   .HasFilter("[PNR] IS NOT NULL");

            // 2. Filtered Unique Index on StripePaymentIntentId for O(1) webhook lookups
            builder.HasIndex(b => b.StripePaymentIntentId)
                   .IsUnique()
                   .HasFilter("[StripePaymentIntentId] IS NOT NULL");

            // 3. Covering Index for User Flight Bookings queries (eliminates Key Lookups & Sort operators)
            builder.HasIndex(b => new { b.UserId, b.CreatedAt })
                   .IsDescending(false, true)
                   .IncludeProperties(b => new
                   {
                       b.OfferId,
                       b.PNR,
                       b.TotalPrice,
                       b.Currency,
                       b.BookingStatus
                   });

            // 4. Unique Filtered Index to prevent concurrent duplicate PendingPayment checkouts for same User + Offer
            builder.HasIndex(b => new { b.UserId, b.OfferId })
                   .IsUnique()
                   .HasFilter("[BookingStatus] = 'PendingPayment'");
        }
    }
}
