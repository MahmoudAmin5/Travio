using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class CheckoutResponseDto
    {
        public string ClientSecret { get; set; } 
        public string StripeIntentId { get; set; } 
    }
}
