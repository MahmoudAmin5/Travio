using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.DTOs.DuffelFlightsDTOs.Requests
{
    public class FlightOrderRequestDto
    {
        public string OfferId { get; set; }
        public List<PassengerDetailsDto> Passengers { get; set; } = new();
    }
    public class PassengerDetailsDto
    {
        public string Title { get; set; } 
        public string GivenName { get; set; } 
        public string FamilyName { get; set; }
        public DateOnly BornOn { get; set; } 
        public string Email { get; set; }
        public string PhoneNumber { get; set; } 
        public string Gender { get; set; } 
    }
}
