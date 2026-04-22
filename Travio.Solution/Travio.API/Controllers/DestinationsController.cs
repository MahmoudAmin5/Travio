using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Domain.Enums;
using Travio.Core.DTOs.DestinationDTO;
using Travio.Core.EntityErrors;
using Travio.Core.Helpers;

namespace Travio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DestinationsController : ControllerBase
    {
        private readonly IDestinationService _destinationService;

        public DestinationsController(IDestinationService destinationService)
        {
            _destinationService = destinationService;
        }

        [HttpGet]
        public async Task<ActionResult<Pagination<DestinationDto>>> GetAll(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] int? cityId = null,
            [FromQuery] int? countryId = null,
            [FromQuery] int? interestId = null,
            [FromQuery] DestinationSortBy sortBy = DestinationSortBy.Rating)
        {
            var result = await _destinationService.GetAllAsync(pageIndex, pageSize, cityId, countryId, interestId, sortBy);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<DestinationDto>> GetById(int id)
        {
            var result = await _destinationService.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiResponse(404));
            return Ok(result);
        }

        [HttpGet("top-rated")]
        public async Task<ActionResult<IEnumerable<DestinationDto>>> GetTopRated([FromQuery] int count = 10)
        {
            var result = await _destinationService.GetTopRatedAsync(count);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<Pagination<DestinationDto>>> Search(
            [FromQuery] string keyword,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new ApiResponse(400, "Keyword is required"));

            var result = await _destinationService.SearchByNameAsync(keyword, pageIndex, pageSize);
            return Ok(result);
        }

        [HttpGet("nearby")]
        public async Task<ActionResult<IEnumerable<DestinationDto>>> GetNearby(
            [FromQuery] decimal latitude,
            [FromQuery] decimal longitude,
            [FromQuery] double radiusKm = 200,
            [FromQuery] int count = 10)
        {
            var result = await _destinationService.GetNearbyAsync(latitude, longitude, radiusKm, count);
            return Ok(result);
        }

        [HttpGet("famous-countries")]
        public async Task<ActionResult<IEnumerable<CountryDto>>> GetFamousCountries()
        {
            var result = await _destinationService.GetFamousCountriesAsync();
            return Ok(result);
        }

        [HttpGet("{destinationId:int}/reviews")]
        [ProducesResponseType(typeof(Pagination<DestinationReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pagination<DestinationReviewDto>>> GetReviews(
            int destinationId,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _destinationService.GetReviewsAsync(destinationId, pageIndex, pageSize, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
        }

        [HttpPost("{destinationId:int}/reviews")]
        [ProducesResponseType(typeof(DestinationReviewMutationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DestinationReviewMutationDto>> UpsertReview(int destinationId, [FromBody] DestinationReviewUpsertDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _destinationService.UpsertMyReviewAsync(destinationId, userId, dto);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
        }

        [HttpPut("{destinationId:int}/reviews/me")]
        [ProducesResponseType(typeof(DestinationReviewMutationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DestinationReviewMutationDto>> UpdateMyReview(int destinationId, [FromBody] DestinationReviewUpsertDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _destinationService.UpdateMyReviewAsync(destinationId, userId, dto);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse(400, ex.Message));
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
        }

        [HttpDelete("{destinationId:int}/reviews/me")]
        [ProducesResponseType(typeof(DestinationReviewDeleteResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DestinationReviewDeleteResultDto>> DeleteMyReview(int destinationId)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _destinationService.DeleteMyReviewAsync(destinationId, userId);
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse(404, ex.Message));
            }
        }
    }
}