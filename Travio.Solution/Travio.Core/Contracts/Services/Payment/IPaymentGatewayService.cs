namespace Travio.Core.Contracts.Services.Payment
{
    /// <summary>
    /// Abstraction over Stripe payment operations.
    /// Prevents direct `new Stripe.PaymentIntentService()` calls inside business logic,
    /// enabling unit testing and respecting Dependency Inversion.
    /// </summary>
    public interface IPaymentGatewayService
    {
        /// <summary>
        /// Creates a Stripe PaymentIntent for the given amount.
        /// </summary>
        /// <param name="amountInCents">Amount in the smallest currency unit (e.g., cents for USD).</param>
        /// <param name="currency">ISO 4217 lowercase currency code (e.g., "usd").</param>
        /// <param name="bookingId">Our internal booking ID — stored in Stripe metadata for webhook correlation.</param>
        /// <param name="bookingType">Booking type label (e.g., "Hotel", "Flight") — stored in Stripe metadata.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The PaymentIntent ID and client secret.</returns>
        Task<PaymentIntentResult> CreatePaymentIntentAsync(
            long amountInCents,
            string currency,
            Guid bookingId,
            string bookingType = "Hotel",
            CancellationToken ct = default);

        /// <summary>
        /// Issues a full refund for a PaymentIntent.
        /// </summary>
        /// <param name="paymentIntentId">The Stripe PaymentIntent ID to refund.</param>
        /// <param name="reason">Refund reason for Stripe's records.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Refund result with success/failure.</returns>
        Task<RefundResult> RefundPaymentAsync(
            string paymentIntentId,
            string reason = "requested_by_customer",
            CancellationToken ct = default);
    }

    /// <summary>Result of creating a Stripe PaymentIntent.</summary>
    public record PaymentIntentResult(string Id, string ClientSecret);

    /// <summary>Result of issuing a Stripe refund.</summary>
    public record RefundResult(string RefundId, bool Success, string? ErrorMessage = null);
}
