namespace Travio.Core.Domain.Enums.Booking
{
    /// <summary>
    /// Represents the lifecycle status of an enterprise-grade hotel booking.
    /// Transitions: PendingPayment → ProcessingWebhook → Confirmed/PaymentFailed. Confirmed → Refunded.
    /// </summary>
    public enum HotelBookingStatus
    {
        /// <summary>User has initiated checkout, waiting for Stripe payment.</summary>
        PendingPayment,

        /// <summary>Stripe webhook received, pre-emptive lock acquired, processing with Hotelbeds.</summary>
        ProcessingWebhook,

        /// <summary>Booking successfully confirmed with Hotelbeds.</summary>
        Confirmed,

        /// <summary>Payment succeeded but Hotelbeds rejected the booking (or payment failed outright).</summary>
        PaymentFailed,

        /// <summary>The booking was confirmed but has since been cancelled/refunded.</summary>
        Refunded
    }
}
