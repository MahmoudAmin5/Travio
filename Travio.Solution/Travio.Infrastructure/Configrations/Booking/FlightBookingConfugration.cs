using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;
using Travio.Core.Domain.Entities.Duffel;

namespace Travio.Infrastructure.Configrations.Booking
{
    public class FlightBookingConfiguration : IEntityTypeConfiguration<FlightBooking>
    {
        public void Configure(EntityTypeBuilder<FlightBooking> builder)
        {
            builder.ToTable("FlightBookings", "Booking");
            builder.HasKey(e => e.Id);
            builder
                .HasIndex(b => b.PNR)
                .IsUnique();
            builder
                .HasIndex(b => b.StripePaymentIntentId)
                .IsUnique();
            builder.Property(e => e.BookingStatus)
                   .HasConversion<string>()
                   .HasMaxLength(50);
            builder.Property(e => e.RowVersion)
           .IsRowVersion();

        }
    }
}
