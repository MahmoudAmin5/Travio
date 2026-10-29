using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Travio.Core.Contracts.Services.Payment;

namespace Travio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IStripeWebhookService _webhookService;
        private readonly string _webhookSecret;

        public StripeController(IStripeWebhookService webhookService, IConfiguration config)
        {
            _webhookService = webhookService;
            _webhookSecret = config["Stripe:WebhookSecret"] ?? string.Empty;
        }

        [HttpPost("webhook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                // 1. Verify the cryptographic signature against our webhook secret
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret,
                    throwOnApiVersionMismatch: false);

                // 2. Check if the event is a successful card charge
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                    {
                        var handled = await _webhookService.ProcessPaymentSuccessAsync(paymentIntent);
                        if (!handled)
                        {
                            // Return 500 if booking record was not yet visible so Stripe retries with backoff
                            return StatusCode(StatusCodes.Status500InternalServerError,
                                new { Error = "Webhook processing could not locate booking record; retry scheduled." });
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                // If the signature fails, it's an invalid/forged request. Block it.
                return BadRequest(new { Error = e.Message });
            }
        }
    }
}
