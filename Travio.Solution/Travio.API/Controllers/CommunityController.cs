using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.DTOs.CommunityDTO;
using Travio.Core.DTOs.GenericResponse;
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
        private readonly IValidator<UploadPostImageDTO> _uploadPostImageValidator;
        private readonly IValidator<CreateCommentDTO> _addCommentValidator;

        public CommunityController(
            IValidator<CreatePostDTO> createPostDtoValidator,
            ICommunityService communityService,
            IValidator<UploadPostImageDTO> uploadPostImageValidator,
             IValidator<CreateCommentDTO> addCommentValidator)
        {
            _createPostDtoValidator = createPostDtoValidator;
            _communityService = communityService;
            _uploadPostImageValidator = uploadPostImageValidator;
            _addCommentValidator = addCommentValidator;
        }

        [HttpPost("create-post")]
        [ProducesResponseType(typeof(ServiceResponse<PostResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]

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
        public async Task<ActionResult> GetCommunityFeed([FromQuery] int pageNumber = 0, [FromQuery] int pageSize = 10)
        {
            var userId = User.GetUserId();

            if (userId is null) return Unauthorized(new ApiResponse(401, "InvalidToken"));

            var feedResponse = await _communityService.GetCommunityFeedAsync(userId, pageNumber, pageSize);

            if (!feedResponse.Success) return BadRequest(new ApiResponse(400, feedResponse.Message));

            return Ok(feedResponse);
        }
        [HttpDelete("posts/{postId}")]
        public async Task<ActionResult> DeletePost(int postId)
        {
            var userId = User.GetUserId();

            if (userId is null) return BadRequest(new ApiResponse(400, "Invalid Token"));

            var Response = await _communityService.DeletePostAsync(postId, userId);

            if (!Response.Success) return BadRequest(new ApiResponse(400, Response.Message));

            return Ok(Response);
        }
        [HttpPost("posts/{postId}/images")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> UploadPostImage(int postId, [FromForm] UploadPostImageDTO dto)
        {
            var validationResult = await _uploadPostImageValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }

            // 2. Authenticate User
            var userId = User.GetUserId();
            if (userId is null)
            {
                return Unauthorized(new ApiResponse(401, "InvalidToken"));
            }


            var response = await _communityService.AddPostImageAsync(postId, userId, dto.Images);

            if (!response.Success)
            {
                return BadRequest(new ApiResponse(400, response.Message));
            }

            return Ok(response);
        }

        [HttpPost("posts/{postId}/toggle-like")]
        [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ToggleLike(int postId)
        {
            var userId = User.GetUserId();
            if (userId is null) return BadRequest(new ApiResponse(400, "Invalid Token"));
            var response = await _communityService.ToggleLikeAsync(postId, userId);
            if (!response.Success) return BadRequest(new ApiResponse(400, response.Message));
            return Ok(response);
        }
        [HttpGet("posts/{postId}")]
        [ProducesResponseType(typeof(ServiceResponse<PostDetailsResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetPostById(int postId)
        {
            var userId = User.GetUserId();
            if (userId is null) return BadRequest(new ApiResponse(400, "Token Is Invalid"));
            var postResponse = await _communityService.GetPostByIdAsync(postId, userId);
            if (postResponse == null) return BadRequest(new ApiResponse(400, postResponse.Message));
            return Ok(postResponse);
        }

        [HttpPost("posts/{postId}/comments")]
        [ProducesResponseType(typeof(ServiceResponse<CommentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> AddComment(int postId, [FromBody] CreateCommentDTO dto)
        {
            var validationResult = await _addCommentValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors);
            }
            var userId = User.GetUserId();
            if (userId == null) return Unauthorized(new ApiResponse(401, "InvalidToken"));

            var response = await _communityService.AddCommentAsync(postId, userId, dto);

            if (!response.Success) return BadRequest(new ApiResponse(400, response.Message));

            return Ok(response);
        }
    }
}


