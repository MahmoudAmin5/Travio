using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class FlightSearchResponseDto
    {
        public string OfferId { get; set; }
        public string TotalOrigin { get; set; }
        public string TotalDestination { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; }
        public int Stops { get; set; }
        public List<FlightSegmentDto> Segments { get; set; }
    }
}
