using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.HotelbedsDTOs.Requests;
using Travio.Core.DTOs.HotelbedsDTOs.Responses;
using Travio.Core.Helpers;

namespace Travio.API.Controllers
{
    /// <summary>
    /// Hotels Module — 7 endpoints for the Hotelbeds APITUDE integration.
    /// Discovery: search, details | Booking: check-rate, book | Management: my-bookings, detail, cancel
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly IHotelbedsService _hotelbedsService;

        public HotelsController(IHotelbedsService hotelbedsService)
        {
            _hotelbedsService = hotelbedsService ?? throw new ArgumentNullException(nameof(hotelbedsService));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 0. SEARCH DESTINATIONS (Autocomplete)
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("destinations/search")]
        [ProducesResponseType(typeof(ServiceResponse<List<Travio.Core.Domain.Entities.Hotelbeds.HotelDestination>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchDestinations([FromQuery] string query, CancellationToken ct)
        {
            var response = await _hotelbedsService.SearchDestinationsAsync(query, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 1. SEARCH — Basic hotel cards with thumbnail images
        // ═══════════════════════════════════════════════════════════════════

        [HttpPost("search")]
        [ProducesResponseType(typeof(ServiceResponse<HotelAvailabilityResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Search([FromBody] HotelAvailabilityRequestDto request, CancellationToken ct)
        {
            if (request is null) return BadRequest(new ApiResponse(400, "Search request body is required."));

            var response = await _hotelbedsService.SearchAvailabilityAsync(request, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 2. HOTEL DETAILS — Full info with all images, facilities, rooms
        // ═══════════════════════════════════════════════════════════════════

        [HttpGet("{hotelCode:int}/details")]
        [ProducesResponseType(typeof(ServiceResponse<HotelDetailResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDetails(int hotelCode, [FromQuery] HotelDetailsQueryDto query, CancellationToken ct)
        {
            if (hotelCode <= 0) return BadRequest(new ApiResponse(400, "A valid hotel code is required."));

            var response = await _hotelbedsService.GetHotelDetailsAsync(hotelCode, query, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 3. CHECK RATE — Confirm price before booking
        // ═══════════════════════════════════════════════════════════════════

        [HttpPost("check-rate")]
        [ProducesResponseType(typeof(ServiceResponse<HotelCheckRateResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckRate([FromBody] HotelCheckRateRequestDto request, CancellationToken ct)
        {
            if (request is null) return BadRequest(new ApiResponse(400, "CheckRate request body is required."));
            if (string.IsNullOrWhiteSpace(request.RateKey)) return BadRequest(new ApiResponse(400, "Rate key is required."));

            var response = await _hotelbedsService.CheckRateAsync(request, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 4. CHECKOUT INIT — Initialize Stripe PaymentIntent
        // ═══════════════════════════════════════════════════════════════════

        [Authorize]
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(ServiceResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Checkout([FromBody] HotelBookingRequestDto request, CancellationToken ct)
        {
            if (request is null) return BadRequest(new ApiResponse(400, "Booking request body is required."));
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse(401, "Unable to identify user."));

            var response = await _hotelbedsService.InitCheckoutAsync(request, userId, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 5. MY BOOKINGS — List user's hotel bookings from DB
        // ═══════════════════════════════════════════════════════════════════

        [Authorize]
        [HttpGet("my-bookings")]
        [ProducesResponseType(typeof(ServiceResponse<List<UserHotelBookingDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyBookings(CancellationToken ct)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse(401, "Unable to identify user."));

            var response = await _hotelbedsService.GetUserBookingsAsync(userId, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 6. BOOKING DETAIL — Live details from Hotelbeds (ownership check)
        // ═══════════════════════════════════════════════════════════════════

        [Authorize]
        [HttpGet("bookings/{reference}")]
        [ProducesResponseType(typeof(ServiceResponse<BookingDetailResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBookingDetail(string reference, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reference)) return BadRequest(new ApiResponse(400, "Booking reference is required."));
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse(401, "Unable to identify user."));

            var response = await _hotelbedsService.GetBookingDetailAsync(reference, userId, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }

        // ═══════════════════════════════════════════════════════════════════
        // 7. CANCEL BOOKING — Cancel via Hotelbeds + update DB (ownership check)
        // ═══════════════════════════════════════════════════════════════════

        [Authorize]
        [HttpDelete("bookings/{reference}")]
        [ProducesResponseType(typeof(ServiceResponse<BookingCancellationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CancelBooking(string reference, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reference)) return BadRequest(new ApiResponse(400, "Booking reference is required."));
            var userId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new ApiResponse(401, "Unable to identify user."));

            var response = await _hotelbedsService.CancelBookingAsync(reference, userId, ct);
            return response.Success ? Ok(response) : BadRequest(new ApiResponse(400, response.Message));
        }
    }
}
