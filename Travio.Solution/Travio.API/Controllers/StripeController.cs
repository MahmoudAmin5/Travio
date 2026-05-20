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
            // You generate this secret using the Stripe CLI during local testing
            _webhookSecret = config["Stripe:WebhookSecret"];
        }
        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                // 1. Verify the signature against your secret
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret
                );

                // 2. Check if the event is a successful card charge
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    // 3. Hand it to the brain
                    await _webhookService.ProcessPaymentSuccessAsync(paymentIntent);
                }

                // 4. Always return 200 OK fast so Stripe knows you received it
                return Ok();
            }
            catch (StripeException e)
            {
                // If the signature fails, it's a fake request. Block it.
                return BadRequest(new { Error = e.Message });
            }
        }
    }
}

