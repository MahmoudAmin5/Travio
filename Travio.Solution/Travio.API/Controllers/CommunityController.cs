using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.Helpers;

namespace Travio.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CommunityController : ControllerBase
    {
        private readonly IValidator<CreatePostDTO> _createPostDtoValidator;
        private readonly ICommunityService _communityService;

        public CommunityController(IValidator<CreatePostDTO> createPostDtoValidator, ICommunityService communityService)
        {
            _createPostDtoValidator = createPostDtoValidator;
            _communityService = communityService;
        }
        [HttpPost("create-post")]
        public async Task<ActionResult> CreatePostAsync(CreatePostDTO model)
        {
            var ValidationResult = await _createPostDtoValidator.ValidateAsync(model);

            if (!ValidationResult.IsValid) return BadRequest(ValidationResult.Errors);

            var userId = User.GetUserId();

            if (userId is null) 
            {
                return Unauthorized(new ApiResponse(401, "InvalidToken"));
            }
            var createdPost = await _communityService.CreatePostAsync(userId, model);

            if (!createdPost.Success) return BadRequest(new ApiResponse(400));
            
            return Ok(createdPost);


        }
        [HttpGet("feed")]
        public async Task<ActionResult> GetCommunityFeed([FromQuery] int pageNumber , [FromQuery] int pageSize)
        {
            var userId = User.GetUserId();

            if (userId is null) return Unauthorized(new ApiResponse(401, "InvalidToken"));

            var feedResponse = await _communityService.GetCommunityFeedAsync(userId, pageNumber, pageSize);

            if (!feedResponse.Success) return BadRequest(new ApiResponse(400, feedResponse.Message));

            return Ok(feedResponse);
        }
    }
}
