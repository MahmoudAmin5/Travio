using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs.Requests
{
    public class CheckoutRequestDto
    {
        public string UserId { get; set; } 
        public string OfferId { get; set; } 

        public List<PassengerDetailsDto> Passengers { get; set; } = new();
    }
}
