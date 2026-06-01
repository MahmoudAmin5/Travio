using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelHotelsDTOs
{
    public class HotelSearchResultDto
    {
        public string PropertyId { get; set; }
        public string HotelName { get; set; }
        public double? Rating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string MainImageUrl { get; set; }
        public decimal StartingPrice { get; set; }
        public string Currency { get; set; }
    }
}
