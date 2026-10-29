using System.ComponentModel.DataAnnotations;
using Travio.Core.Domain.Enums.Booking;

namespace Travio.Core.Domain.Entities.Duffel
{
    public class FlightBooking
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public string UserId { get; set; } = string.Empty;

        public string OfferId { get; set; } = string.Empty; // The Duffel Offer ID

        public string? PNR { get; set; } // Null until confirmed by Duffel

        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = string.Empty;

        public string? StripePaymentIntentId { get; set; }

        public FlightBookingStatus BookingStatus { get; set; } = FlightBookingStatus.PendingPayment;
        public string PassengersJson { get; set; } = string.Empty;
        public string? FailureReason { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
