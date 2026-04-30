using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class FlightSegmentDetailsDto
    {
        public string AirlineName { get; set; }
        public string AirlineLogoUrl { get; set; }
        public string FlightNumber { get; set; }

       
        public string AircraftName { get; set; }

        public string OriginAirport { get; set; }
        public string DepartureTime { get; set; }

        public string DestinationAirport { get; set; }
        public string ArrivalTime { get; set; }

        public string SegmentDuration { get; set; }
    }
}
