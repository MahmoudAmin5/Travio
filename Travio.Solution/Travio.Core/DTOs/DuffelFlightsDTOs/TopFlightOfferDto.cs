using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class TopFlightOfferDto
    {
        public string DestinationName { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string TravelDate { get; set; }
        public string ImageUrl { get; set; }
        public decimal CheapestPrice { get; set; }
        public string Currency { get; set; }
        public string AirlineName { get; set; }
        public string OfferId { get; set; }
    }
}
