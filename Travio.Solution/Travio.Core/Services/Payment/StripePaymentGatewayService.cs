using Microsoft.Extensions.Logging;
using Stripe;
using Travio.Core.Contracts.Services.Payment;

namespace Travio.Core.Services.Payment
{
    /// <summary>
    /// Thin wrapper over the Stripe SDK for payment operations.
    /// Enforces deterministic Stripe Idempotency-Keys on all mutations to eliminate double-charge
    /// and duplicate-refund race conditions during concurrent requests or network retries.
    /// </summary>
    public class StripePaymentGatewayService : IPaymentGatewayService
    {
        private readonly ILogger<StripePaymentGatewayService> _logger;

        public StripePaymentGatewayService(ILogger<StripePaymentGatewayService> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
            long amountInCents,
            string currency,
            Guid bookingId,
            string bookingType = "Hotel",
            CancellationToken ct = default)
        {
            var service = new PaymentIntentService();
            var normalizedCurrency = currency.ToLowerInvariant();
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = normalizedCurrency,
                Metadata = new Dictionary<string, string>
                {
                    { "BookingId", bookingId.ToString() },
                    { "BookingType", bookingType }
                }
            };

            // Deterministic Stripe Idempotency Key tied to BookingId + BookingType + Amount + Currency
            var requestOptions = new RequestOptions
            {
                IdempotencyKey = $"pi_create_{bookingType.ToLowerInvariant()}_{bookingId:N}_{amountInCents}_{normalizedCurrency}"
            };

            var paymentIntent = await service.CreateAsync(options, requestOptions, cancellationToken: ct);
            _logger.LogInformation(
                "Stripe PaymentIntent created: {IntentId} for BookingId {BookingId} ({BookingType}), Amount: {Amount} {Currency}.",
                paymentIntent.Id, bookingId, bookingType, amountInCents, normalizedCurrency);

            return new PaymentIntentResult(paymentIntent.Id, paymentIntent.ClientSecret);
        }

        public async Task<RefundResult> RefundPaymentAsync(
            string paymentIntentId,
            string reason = "requested_by_customer",
            Guid? bookingId = null,
            CancellationToken ct = default)
        {
            try
            {
                var service = new RefundService();
                var options = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Reason = reason
                };

                // Deterministic Stripe Idempotency Key prevents duplicate refunds across concurrent retries
                var idempotencyKey = bookingId.HasValue
                    ? $"refund_{bookingId.Value:N}_{paymentIntentId}"
                    : $"refund_{paymentIntentId}";

                var requestOptions = new RequestOptions
                {
                    IdempotencyKey = idempotencyKey
                };

                var refund = await service.CreateAsync(options, requestOptions, cancellationToken: ct);
                _logger.LogInformation(
                    "Stripe Refund issued: {RefundId} for Intent {IntentId}.",
                    refund.Id, paymentIntentId);

                return new RefundResult(refund.Id, true);
            }
            catch (StripeException ex)
            {
                _logger.LogCritical(ex,
                    "CRITICAL: Stripe Refund FAILED for Intent {IntentId}. Manual intervention required.",
                    paymentIntentId);
                return new RefundResult(string.Empty, false, ex.Message);
            }
        }
    }
}
