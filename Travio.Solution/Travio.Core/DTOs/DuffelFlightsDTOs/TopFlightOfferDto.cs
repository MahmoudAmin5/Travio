using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class TopFlightOfferDto
    {

        public string OfferId { get; set; }
        public string AirlineName { get; set; }
        public string ImageUrl { get; set; }

        public string Origin { get; set; }
        public string OriginCityName { get; set; }

        public string Destination { get; set; }
        public string DestinationCityName { get; set; }

        public string Duration { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineLogoUrl { get; set; }

        public int Stops { get; set; }

        public decimal CheapestPrice { get; set; }
        public string Currency { get; set; }
    }

}
