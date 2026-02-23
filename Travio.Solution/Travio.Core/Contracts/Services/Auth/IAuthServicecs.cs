using System.IdentityModel.Tokens.Jwt;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Entities.Enums;
using Travio.Core.DTOs;
using Travio.Core.DTOs.GenericResponse;

namespace Travio.Core.Contracts.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthDTO> RegisterAsync(RegisterDTO model);
        Task<AuthDTO> GetTokenAsync(LoginDTO model);
        Task<string> AddRoleAsync(AddRoleDTO model);
        Task<AuthDTO> RefreshTokenAsync(RefreshTokenRequestDto token);
        Task<bool> RevokeTokenAsync(string token);
        Task<JwtSecurityToken> JwtSecurityTokenAsync(ApplicationUser User);
        Task<AuthDTO> LoginWithGoogleAsync(string idToken);
        Task<string> ForgotPasswordAsync(string email);
        Task<SendOtpResponseDto> SendEmailConfirmationAsync(SendOtpRequestDto model);
        Task<VerifyOtpResponseDto> ConfirmEmailAsync(VerifyOtpRequestDto model);
        Task<VerifyOtpResponseDto> VerifyOtpAsync(ApplicationUser user, string Otp, AuthCodeType CodeType);
        Task DeleteOtps();
        Task<ServiceResponse<string>> ResetPasswordAsync(ResetPasswordDTO Model);
    }
}
