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
     
        PendingPayment,
        ProcessingWebhook,
        Confirmed,
        PaymentFailed,
        SupplierFailed,     
        RefundIssued,
        Refunded
    }
}
