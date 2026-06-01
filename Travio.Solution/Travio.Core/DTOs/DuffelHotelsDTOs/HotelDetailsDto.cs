using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelHotelsDTOs
{
    public class HotelDetailsDto
    {
        public string PropertyId { get; set; }
        public string HotelName { get; set; }
        public double? Rating { get; set; }
        public string Description { get; set; }
        public List<string> Photos { get; set; } = new();
        public List<string> Amenities { get; set; } = new();
        public List<RoomRateDto> AvailableRooms { get; set; } = new();
    }
}
