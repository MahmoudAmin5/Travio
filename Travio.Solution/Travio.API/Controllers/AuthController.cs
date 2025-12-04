using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Travio.API.Errors;
using Travio.Core.Contracts.Services;
using Travio.Core.Domain.Entities.Enums;
using Travio.Core.DTOs;

namespace Travio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IAuthService _authService;
        private readonly IEmailSender _emailSender;

        public AuthController(IAuthService authService, IEmailSender emailSender)
        {
            _authService = authService;
            _emailSender = emailSender;
        }


        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterDTO model)
        {

            var result = await _authService.RegisterAsync(model);

            if (!result.IsAuthenticated)
                return Unauthorized(new ApiResponse(401, result.Message));
            if (!string.IsNullOrEmpty(result.RefreshToken))
                SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);
            return Ok(result);
        }
        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDTO model)
        {

            var result = await _authService.GetTokenAsync(model);

            if (!result.IsAuthenticated)
                return BadRequest(new ApiResponse(401, result.Message));
            if (!string.IsNullOrEmpty(result.RefreshToken))
                SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);
            return Ok(result);
        }
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLoginAsync([FromBody] GoogleLoginDTO model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.LoginWithGoogleAsync(model.IdToken);
            if (!result.IsAuthenticated) return BadRequest(new ApiResponse(401, result.Message));
            if (!string.IsNullOrEmpty(result.RefreshToken))
                SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);
            return Ok(result);

        }
        [HttpGet("refreshToken")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.IsAuthenticated)
                return BadRequest(result);
            if (!string.IsNullOrEmpty(result.RefreshToken))
                SetRefreshTokenInCookie(result.RefreshToken, result.RefreshTokenExpiration);

            return Ok(result);
        }
        [HttpPost("Logout")]
        public async Task<IActionResult> RevokeToken([FromBody] LogoutDTO model)
        {
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest("Token is required!");

            var result = await _authService.RevokeTokenAsync(token);

            if (!result)
                return BadRequest("Token is invalid!");

            return Ok();
        }

        private void SetRefreshTokenInCookie(string refreshToken, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expires.ToLocalTime(),
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.None
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
        [HttpPost("send-test")]
        public async Task<IActionResult> SendTest([FromQuery] string to)
        {
            if (string.IsNullOrWhiteSpace(to)) return BadRequest("Provide ?to=email@example.com");

            var subject = "Travio — Test Email Connection";
            var currentTime = DateTime.UtcNow.ToString("f"); // تنسيق وقت مقروء

            var html = $$"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Travio Email</title>
                    <style>
                        body { margin: 0; padding: 0; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f6f9fc; }
                        table { border-collapse: collapse; width: 100%; }
                        .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); margin-top: 40px; margin-bottom: 40px; }
                        .header { background: linear-gradient(135deg, #0061f2 0%, #00c6f7 100%); padding: 40px 20px; text-align: center; }
                        .header h1 { color: #ffffff; margin: 0; font-size: 32px; font-weight: 700; letter-spacing: 1px; }
                        .content { padding: 40px 30px; color: #4a5568; line-height: 1.6; }
                        .content h2 { color: #2d3748; font-size: 22px; margin-top: 0; }
                        .btn { display: inline-block; padding: 12px 24px; background-color: #0061f2; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 20px; }
                        .footer { background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #a0aec0; border-top: 1px solid #e2e8f0; }
            
                        @media only screen and (max-width: 600px) {
                            .content { padding: 20px; }
                            .header { padding: 30px 15px; }
                        }
                    </style>
                </head>
                <body>
                    <table role="presentation">
                        <tr>
                            <td align="center">
                                <div class="container">
                                    <div class="header">
                                        <h1>Travio ✈️</h1>
                                    </div>

                                    <div class="content">
                                        <h2>Hello there!</h2>
                                        <p>
                                            We are thrilled to confirm that your <strong>Travio</strong> notification system is working perfectly. 
                                            This email was sent directly from your .NET backend.
                                        </p>
                            
                                        <div style="background-color: #ebf8ff; border-left: 4px solid #4299e1; padding: 15px; margin: 20px 0; border-radius: 4px;">
                                            <p style="margin: 0; color: #2b6cb0; font-size: 14px;">
                                                <strong>Server Time:</strong> {{currentTime}} (UTC)
                                            </p>
                                        </div>

                                        <p>Ready to explore the world? Check out our latest destinations.</p>

                                        <a href="#" class="btn" style="color: #ffffff;">Explore Now</a>
                                    </div>

                                    <div class="footer">
                                        <p>&copy; 2025 Travio Inc. All rights reserved.</p>
                                        <p>Cairo, Egypt</p>
                                    </div>
                                </div>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>
                """;

            try
            {
                await _emailSender.SendEmailAsync(to, subject, html);
                return Ok("Email sent successfully with HTML template.");
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Sending failed: {ex.Message}");
            }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPasswordAsync(ForgotPasswordDTO model)
        {
            var resultMessage = await _authService.ForgotPasswordAsync(model.Email);
            return Ok(resultMessage);
        }

        //[HttpPost("send-verify-email-otp")]
        //public async Task<IActionResult> SendVerifyEmailOtp([FromBody] SendOtpRequestDto model)
        //{
        //    if (model == null || string.IsNullOrWhiteSpace(model.Target))
        //        return BadRequest(new SendOtpResponseDto(VerifyOtpStatus.Invalid, "Target is required", null));

            
        //    var result = await _authService.SendEmailConfirmationAsync(model);

        //    //返回成功 always true for rate-limited / not-existing? adjust in service
        //    return Ok(new SendOtpResponseDto(true, result.Message, result.ExpiresOn));
        //}
    }
}
