   using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.DTOs.ProfileDTOs;
using Travio.Core.Helpers;
using Travio.Core.Services;

namespace Travio.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }
        [HttpGet]
        public async Task <ActionResult> GetMyProfile()
        {
            var userId = User.GetUserId();
            var result = await _profileService.GetUserProfileAsync(userId);
            if (result is null) return BadRequest(new ApiResponse(400, "Invalid User"));
            return Ok(result);
        }
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO model)
        {
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            
            var userId = User.GetUserId();

           
            var result = await _profileService.UpdateProfileAsync(userId, model);

            if (!result.Success)
                return BadRequest(result);

            
            return Ok(result);
        }
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            
            var userId = User.GetUserId();

           
            var result = await _profileService.UploadProfileImageAsync(userId, file);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
