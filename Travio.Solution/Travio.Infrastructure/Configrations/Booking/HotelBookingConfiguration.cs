using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.Hotelbeds;

namespace Travio.Infrastructure.Configrations.Booking
{
    /// <summary>
    /// EF Core Fluent API configuration for the <see cref="HotelBooking"/> entity.
    /// Placed in the Infrastructure layer alongside existing entity configurations.
    /// Discovered automatically via <c>ApplyConfigurationsFromAssembly</c> in the DbContext.
    /// </summary>
    public class HotelBookingConfiguration : IEntityTypeConfiguration<HotelBooking>
    {
        public void Configure(EntityTypeBuilder<HotelBooking> builder)
        {
            // ── Table & Schema ────────────────────────────────────────────────
            builder.ToTable("HotelBookings", "Booking");

            // ── Primary Key ───────────────────────────────────────────────────
            builder.HasKey(e => e.Id);

            // ── Indexes ───────────────────────────────────────────────────────
            // Index on UserId for efficient user-specific booking queries
            builder.HasIndex(b => b.UserId);

            // Unique index on HotelbedsReference (nullable — only set on confirmed bookings)
            builder.HasIndex(b => b.HotelbedsReference)
                   .IsUnique()
                   .HasFilter("[HotelbedsReference] IS NOT NULL");

            // ── Property Configurations ───────────────────────────────────────

            builder.Property(e => e.UserId)
                   .IsRequired()
                   .HasMaxLength(450); // Match ASP.NET Identity user ID length

            builder.Property(e => e.HotelName)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(e => e.TotalPrice)
                   .HasPrecision(18, 2); // Standard financial precision

            builder.Property(e => e.Currency)
                   .IsRequired()
                   .HasMaxLength(3); // ISO 4217 currency codes are 3 chars

            builder.Property(e => e.HotelbedsReference)
                   .HasMaxLength(100);

            builder.Property(e => e.RateKey)
                   .HasMaxLength(2000); // Rate keys can be very long

            // Serialized booking request — stored so the webhook can reconstruct the Hotelbeds call
            builder.Property(e => e.GuestDataJson)
                   .HasMaxLength(8000);

            // Store enum as string for readability in the database
            builder.Property(e => e.BookingStatus)
                   .HasConversion<string>()
                   .HasMaxLength(50);

            // ── Concurrency Token ─────────────────────────────────────────────
            // CRITICAL: Enables optimistic concurrency via SQL Server rowversion.
            // EF Core checks this value in WHERE clauses on UPDATE/DELETE.
            builder.Property(e => e.RowVersion)
                   .IsRowVersion();
        }
    }
}
