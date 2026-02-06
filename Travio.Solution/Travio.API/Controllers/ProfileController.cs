using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services;
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
    }
}
