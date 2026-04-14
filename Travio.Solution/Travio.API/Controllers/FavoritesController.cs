using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.Helpers;

namespace Travio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IUserFavoriteService _favoriteService;

    public FavoritesController(IUserFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }


    [HttpPost("{destinationId}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> AddFavorite(int destinationId)
    {
        var userId = User.GetUserId();

        var result = await _favoriteService.AddFavoriteAsync(userId, destinationId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{destinationId}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFavorite(int destinationId)
    {
        var userId = User.GetUserId();

        var result = await _favoriteService.DeleteFavoriteAsync(userId, destinationId);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<Pagination<GetFavDestinationResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserFavorites([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();

        var result = await _favoriteService.GetUserFavoritesAsync(pageIndex, pageSize, userId);

        return Ok(result);
    }
}