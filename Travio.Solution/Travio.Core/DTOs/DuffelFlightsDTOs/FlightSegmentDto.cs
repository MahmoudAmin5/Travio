using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class FlightSegmentDto
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string AirlineName { get; set; }
        public string FlightNumber { get; set; }

        // --- NEW SEGMENT FIELDS ---
        public string OriginCityName { get; set; }
        public string DestinationCityName { get; set; }
        public string SegmentDuration { get; set; }
        public string AirlineLogoUrl { get; set; }
        // --------------------------
    }
}

