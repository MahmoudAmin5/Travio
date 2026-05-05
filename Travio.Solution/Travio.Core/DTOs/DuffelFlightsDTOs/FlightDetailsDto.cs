using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs
{
    public class FlightDetailsDto
    {
        public string OfferId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TaxAmount { get; set; }
        public string Currency { get; set; }
        public string TotalDuration { get; set; }
        public string OriginCityName { get; set; }
        public string DestinationCityName { get; set; }
        public int CheckedBags { get; set; }
        public bool IsRefundable { get; set; }
        public decimal? RefundPenaltyAmount { get; set; }

        public List<FlightSegmentDetailsDto> Segments { get; set; } = new();
    }

}
