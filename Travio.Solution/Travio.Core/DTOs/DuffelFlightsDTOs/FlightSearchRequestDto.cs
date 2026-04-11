using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class FlightSearchRequestDto
    {
        public string Origin { get; set; } 
        public string Destination { get; set; } 
        public string DepartureDate { get; set; }
        public int NumberOfAdults { get; set; }
    }
}
