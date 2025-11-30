using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services;
using Travio.Core.DTOs;

namespace Travio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

         private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }


        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterDTO model)
        {
           
            var result = await _authService.RegisterAsync(model);

            if (!result.IsAuthenticated)
                return Unauthorized(new ApiResponse(401,result.Message));

            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDTO model)
        {

            var result = await _authService.GetTokenAsync(model);

            if (!result.IsAuthenticated)
                return BadRequest(new ApiResponse(401, result.Message));

            return Ok(result);
        }
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLoginAsync([FromBody] GoogleLoginDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.LoginWithGoogleAsync(model.IdToken);
            if (!result.IsAuthenticated) return BadRequest(new ApiResponse(401, result.Message));
            return Ok(result);
            // test
        }
       
    }
}
