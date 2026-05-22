using System.ComponentModel.DataAnnotations;
using Travio.Core.Domain.Enums.Booking;

namespace Travio.Core.Domain.Entities.Hotelbeds
{
    /// <summary>
    /// EF Core entity representing a hotel booking made through the Hotelbeds APITUDE API.
    /// Maps to the "HotelBookings" table in the "Booking" schema.
    /// 
    /// CRITICAL: Uses a concurrency token (RowVersion) for optimistic concurrency control.
    /// This prevents race conditions where two requests attempt to modify the same booking simultaneously.
    /// </summary>
    public class HotelBooking
    {
        /// <summary>Primary key — uses UUID v7 for time-ordered uniqueness.</summary>
        public Guid Id { get; set; } = Guid.CreateVersion7();

        /// <summary>The ID of the authenticated user who owns this booking.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>The Hotelbeds hotel code (numeric identifier for the property).</summary>
        public int HotelId { get; set; }

        /// <summary>The hotel name at the time of booking (denormalized for display).</summary>
        public string HotelName { get; set; } = string.Empty;

        /// <summary>Check-in date.</summary>
        public DateOnly CheckIn { get; set; }

        /// <summary>Check-out date.</summary>
        public DateOnly CheckOut { get; set; }

        /// <summary>Total price at the time of booking confirmation.</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>ISO 4217 currency code (e.g., "USD", "EUR").</summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>The Stripe PaymentIntent ID for this booking.</summary>
        public string? StripePaymentIntentId { get; set; }

        /// <summary>
        /// The unique booking reference returned by Hotelbeds upon successful booking.
        /// Null until the booking is confirmed.
        /// </summary>
        public string? HotelbedsReference { get; set; }

        /// <summary>The rate key used for the CheckRate/Booking call — stored for audit trail.</summary>
        public string RateKey { get; set; } = string.Empty;

        /// <summary>Current lifecycle status of this booking.</summary>
        public HotelBookingStatus BookingStatus { get; set; } = HotelBookingStatus.PendingPayment;

        /// <summary>Number of rooms booked.</summary>
        public int RoomCount { get; set; } = 1;

        /// <summary>
        /// Serialized JSON of the original HotelBookingRequestDto.
        /// Stored at checkout time so the webhook can reconstruct the full Hotelbeds booking
        /// request with real guest names, room details, and pax information.
        /// </summary>
        public string? GuestDataJson { get; set; }

        /// <summary>
        /// The wholesale net price in EUR as returned by Hotelbeds CheckRate.
        /// Frozen at checkout time — never recalculated.
        /// Formula: TotalPrice = WholesaleNetEur × 1.15 (markup) × ExchangeRateAtCheckout.
        /// </summary>
        public decimal WholesaleNetEur { get; set; }

        /// <summary>
        /// EUR → USD exchange rate locked at checkout time.
        /// Prevents financial drift between checkout and webhook fulfillment.
        /// </summary>
        public decimal ExchangeRateAtCheckout { get; set; }

        /// <summary>
        /// Human-readable failure reason for support dashboards.
        /// Set when BookingStatus transitions to SupplierFailed, PaymentFailed, etc.
        /// </summary>
        public string? FailureReason { get; set; }
        /// <summary>
        /// CRITICAL: Optimistic concurrency token managed by SQL Server.
        /// EF Core will include this in WHERE clauses on UPDATE/DELETE to detect conflicts.
        /// If another process modified the row, a DbUpdateConcurrencyException is thrown.
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        /// <summary>UTC timestamp when this booking record was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the last modification (null if never updated).</summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
