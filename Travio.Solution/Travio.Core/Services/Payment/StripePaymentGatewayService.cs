using Microsoft.Extensions.Logging;
using Stripe;
using Travio.Core.Contracts.Services.Payment;

namespace Travio.Core.Services.Payment
{
    /// <summary>
    /// Thin wrapper over the Stripe SDK for payment operations.
    /// Injected via DI so business logic doesn't depend on concrete Stripe classes.
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
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = currency,
                Metadata = new Dictionary<string, string>
                {
                    { "BookingId", bookingId.ToString() },
                    { "BookingType", bookingType }
                }
            };

            var paymentIntent = await service.CreateAsync(options, cancellationToken: ct);
            _logger.LogInformation(
                "Stripe PaymentIntent created: {IntentId} for BookingId {BookingId}, Amount: {Amount} {Currency}.",
                paymentIntent.Id, bookingId, amountInCents, currency);

            return new PaymentIntentResult(paymentIntent.Id, paymentIntent.ClientSecret);
        }

        public async Task<RefundResult> RefundPaymentAsync(
            string paymentIntentId,
            string reason = "requested_by_customer",
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

                var refund = await service.CreateAsync(options, cancellationToken: ct);
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
