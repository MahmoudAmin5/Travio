namespace Travio.Core.Domain.Enums.Booking
{
    /// <summary>
    /// Represents the lifecycle status of an enterprise-grade hotel booking.
    /// Transitions: PendingPayment → ProcessingWebhook → Confirmed | SupplierFailed.
    ///              SupplierFailed → RefundIssued → Refunded.
    ///              Confirmed → Refunded.
    /// </summary>
    public enum HotelBookingStatus
    {
        /// <summary>User has initiated checkout, waiting for Stripe payment.</summary>
        PendingPayment,

        /// <summary>Stripe webhook received, pre-emptive lock acquired, processing with Hotelbeds.</summary>
        ProcessingWebhook,

        /// <summary>Booking successfully confirmed with Hotelbeds.</summary>
        Confirmed,

        /// <summary>Stripe payment was declined or failed before reaching the supplier.</summary>
        PaymentFailed,

        /// <summary>
        /// Payment succeeded but Hotelbeds rejected the booking (rate expired, allotment sold out, etc.).
        /// OPERATIONAL ALERT: The customer was charged — a compensating refund is needed.
        /// </summary>
        SupplierFailed,

        /// <summary>
        /// A compensating refund has been submitted to Stripe but has not yet settled.
        /// Transitions to Refunded once Stripe confirms (via charge.refunded webhook, or manual check).
        /// </summary>
        RefundIssued,

        /// <summary>The booking was confirmed but has since been cancelled/refunded.</summary>
        Refunded
    }
}
