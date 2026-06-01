using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.Core.Contracts.Services.DuffelHotels;
using Travio.Core.DTOs.DuffelHotelsDTOs.Requests;

namespace Travio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DuffelHotelsController : ControllerBase
    {
        private readonly IDuffelHotelsService _hotelsService;

        public DuffelHotelsController(IDuffelHotelsService hotelsService)
        {
            _hotelsService = hotelsService;
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchHotels([FromQuery] HotelSearchRequestDto request)
        {

            if (string.IsNullOrWhiteSpace(request.CheckInDate) || string.IsNullOrWhiteSpace(request.CheckOutDate))
            {
                return BadRequest(new { success = false, message = "Check-in and Check-out dates are required." });
            }

            var response = await _hotelsService.SearchHotelsAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    

        [HttpGet("details/{searchResultId}")]
        public async Task<IActionResult> GetHotelDetails(string searchResultId)
        {
            var response = await _hotelsService.GetHotelDetailsAsync(searchResultId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
