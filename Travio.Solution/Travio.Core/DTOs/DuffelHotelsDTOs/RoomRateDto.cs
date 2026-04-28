using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelHotelsDTOs
{
    public class RoomRateDto
    {
        public string RateId { get; set; } 
        public string RoomName { get; set; }
        public string BoardType { get; set; } 
        public decimal Price { get; set; }
        public string Currency { get; set; }
    }
}
