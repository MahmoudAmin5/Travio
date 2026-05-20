using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.TripPlanerDTOs;
using Travio.Core.Helpers;

namespace Travio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TripController : ControllerBase
{
    private readonly ISavedTripService _tripService;

    public TripController(ISavedTripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>
    /// Get all saved trips for the authenticated user (paginated).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<Pagination<SavedTripSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTrips([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();
        var result = await _tripService.GetUserTripsAsync(pageIndex, pageSize, userId);
        return Ok(result);
    }

    /// <summary>
    /// Get only favorite trips for the authenticated user (paginated).
    /// </summary>
    [HttpGet("favorites")]
    [ProducesResponseType(typeof(ServiceResponse<Pagination<SavedTripSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFavoriteTrips([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();
        var result = await _tripService.GetUserFavoriteTripsAsync(pageIndex, pageSize, userId);
        return Ok(result);
    }

    /// <summary>
    /// Get full trip detail including days, activities, and hotels.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<SavedTripDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<SavedTripDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTripById(int id)
    {
        var userId = User.GetUserId();
        var result = await _tripService.GetTripByIdAsync(id, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Toggle favorite status on a trip.
    /// </summary>
    [HttpPost("{id}/favorite")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        var userId = User.GetUserId();
        var result = await _tripService.ToggleFavoriteAsync(id, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Delete a saved trip.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrip(int id)
    {
        var userId = User.GetUserId();
        var result = await _tripService.DeleteTripAsync(id, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
