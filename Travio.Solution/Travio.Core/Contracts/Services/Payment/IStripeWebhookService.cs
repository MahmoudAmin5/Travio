using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Contracts.Services.Payment
{
    public interface IStripeWebhookService
    {
        Task<bool> ProcessPaymentSuccessAsync(Stripe.PaymentIntent paymentIntent);
    }
}
