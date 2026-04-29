using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.DTOs.DuffelFlightsDTOs;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightBookingController : ControllerBase
    {
        private readonly IDuffelFlightBookingService _flightBookingService;

        public FlightBookingController(IDuffelFlightBookingService flightBookingService)
        {
            _flightBookingService = flightBookingService;
        }

        [HttpGet("flights/search")]
        [ProducesResponseType(typeof(ServiceResponse<List<FlightSearchResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> SearchFlights([FromQuery] FlightSearchRequestDto request)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Origin) || string.IsNullOrWhiteSpace(request.Destination))
            {
                return BadRequest(new ApiResponse(400, "Origin and Destination are required."));
            }

            var response = await _flightBookingService.SearchFlightsAsync(request);

            if (!response.Success)
            {
                return BadRequest(new ApiResponse(400, response.Message));
            }

            return Ok(response);
        }
        [HttpGet("top-offers")]
        [ProducesResponseType(typeof(ServiceResponse<List<TopFlightOfferDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTopOffers()
        {
            var response = await _flightBookingService.GetTopOffersAsync();

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
