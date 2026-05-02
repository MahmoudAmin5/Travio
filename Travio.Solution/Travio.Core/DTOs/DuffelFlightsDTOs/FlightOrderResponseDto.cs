using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class FlightOrderResponseDto
    {
        public string DuffelOrderId { get; set; }
        public string PNR { get; set; }
        public string BookingStatus { get; set; }
    }
}