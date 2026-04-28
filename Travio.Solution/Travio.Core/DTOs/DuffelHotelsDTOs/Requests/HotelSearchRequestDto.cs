using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelHotelsDTOs.Requests
{
    public class HotelSearchRequestDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int RadiusKm { get; set; } = 10; // Default to a 10km search area
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public int Adults { get; set; } = 2;
        public int Rooms { get; set; } = 1;
    }
}
