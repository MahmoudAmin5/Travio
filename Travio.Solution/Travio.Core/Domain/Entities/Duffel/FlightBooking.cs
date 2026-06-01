using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Enums.Booking;

namespace Travio.Core.Domain.Entities.Duffel
{
    public class FlightBooking
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        public string UserId { get; set; }

       
        public string OfferId { get; set; } // The Duffel Offer ID

       
        public string PNR { get; set; } // e.g., "XYZ789"

      
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; }

       
        public string StripePaymentIntentId { get; set; }

       
        public FlightBookingStatus BookingStatus { get; set; } = FlightBookingStatus.PendingPayment;
        public string PassengersJson { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

    }
}

