using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Enums;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.EntityErrors;
using Travio.Core.Helpers;
using Travio.Core.Setting;

namespace Travio.Core.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IGenericRepository<UserCode> _userCodeRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly JWT _jwt;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IOptions<JWT> jwt,
            IGoogleAuthService googleAuthService,
            IGenericRepository<UserCode> userCodeRepo,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _googleAuthService = googleAuthService;
            _userCodeRepo = userCodeRepo;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _jwt = jwt.Value;
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var tokenHash = HashToken(token);
            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash));

            if (user == null) return false;

            var refreshToken = user.RefreshTokens.Single(t => t.TokenHash == tokenHash);

            if (!refreshToken.IsActive) return false;

            refreshToken.RevokedOn = DateTime.UtcNow;
            refreshToken.RevokeReason = "Revoked by user";

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return false;
            }

            return true;
        }

        public async Task<AuthDTO> RefreshTokenAsync(RefreshTokenRequestDto RefreshToken)
        {
            var authModel = new AuthDTO();
            var token = RefreshToken.RefreshToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                authModel.Message = "Token is required";
                return authModel;
            }

            var tokenHash = HashToken(token);

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash));

            if (user == null)
            {
                authModel.Message = "Invalid token";
                return authModel;
            }

            var refreshToken = user.RefreshTokens.Single(t => t.TokenHash == tokenHash);

            if (!refreshToken.IsActive)
            {
                // Reuse detection: if token was revoked, revoke all tokens for this user (security)
                foreach (var t in user.RefreshTokens.Where(x => x.IsActive))
                {
                    t.RevokedOn = DateTime.UtcNow;
                    t.RevokeReason = "Revoked due to reuse detection";
                }

                await _userManager.UpdateAsync(user);

                authModel.Message = "Inactive token";
                return authModel;
            }

            // revoke current token (rotate)
            refreshToken.RevokedOn = DateTime.UtcNow;
            refreshToken.RevokeReason = "Rotated";

            // create new plain token to give to client
            var newPlain = GenerateRandomTokenPlain();
            var newEntity = CreateRefreshTokenEntity(newPlain, daysValid: 10);
            user.RefreshTokens.Add(newEntity);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                authModel.Message = "Server error";
                return authModel;
            }

            // create new access token
            var jwtToken = await JwtSecurityTokenAsync(user);

            authModel.IsAuthenticated = true;
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            authModel.Email = user.Email;
            authModel.Username = user.UserName;
            var roles = await _userManager.GetRolesAsync(user);
            authModel.Roles = roles.ToList();

            authModel.RefreshToken = newPlain;
            authModel.RefreshTokenExpiration = newEntity.ExpiresOn;

            return authModel;
        }

        public async Task<AuthDTO> GetTokenAsync(LoginDTO model)
        {
            var authModel = new AuthDTO();

            var user = await _userManager.Users
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.Email == model.Email_or_username || u.UserName == model.Email_or_username);

            if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                authModel.Message = "Email or Password is incorrect!";
                return authModel;
            }

            var jwtSecurityToken = await JwtSecurityTokenAsync(user);
            var rolesList = await _userManager.GetRolesAsync(user);

            authModel.IsAuthenticated = true;
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            authModel.Email = user.Email;
            authModel.Username = user.UserName;
            authModel.ExpiresOn = jwtSecurityToken.ValidTo;
            authModel.Roles = rolesList.ToList();

            var active = user.RefreshTokens.FirstOrDefault(t => t.IsActive);
            if (active != null)
            {
                var newPlain = GenerateRandomTokenPlain();
                var newEntity = CreateRefreshTokenEntity(newPlain, daysValid: 10);
                active.RevokedOn = DateTime.UtcNow;
                active.RevokeReason = "Rotated on login";

                user.RefreshTokens.Add(newEntity);
                await _userManager.UpdateAsync(user);
                authModel.RefreshToken = newPlain;
                authModel.RefreshTokenExpiration = newEntity.ExpiresOn;
            }
            else
            {
                var plain = GenerateRandomTokenPlain();
                var entity = CreateRefreshTokenEntity(plain, daysValid: 10);
                user.RefreshTokens.Add(entity);
                await _userManager.UpdateAsync(user);
                authModel.RefreshToken = plain;
                authModel.RefreshTokenExpiration = entity.ExpiresOn;
            }

            return authModel;
        }

        public async Task<string> AddRoleAsync(AddRoleDTO model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user is null || !await _roleManager.RoleExistsAsync(model.Role))
                return "Invalid user ID or Role";

            if (await _userManager.IsInRoleAsync(user, model.Role))
                return "User already assigned to this role";

            var result = await _userManager.AddToRoleAsync(user, model.Role);

            return result.Succeeded ? string.Empty : "Something went wrong";
        }

        public async Task<AuthDTO> RegisterAsync(RegisterDTO model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) is not null)
                return new AuthDTO { Message = "Email is already registered!" };

            if (await _userManager.FindByNameAsync(model.Username) is not null)
                return new AuthDTO { Message = "Username is already registered!" };

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(",", result.Errors.Select(e => e.Description));
                return new AuthDTO { Message = errors };
            }

            await _userManager.AddToRoleAsync(user, "User");

            var jwtSecurityToken = await JwtSecurityTokenAsync(user);

            var plain = GenerateRandomTokenPlain();
            var entity = CreateRefreshTokenEntity(plain, daysValid: 10);
            user.RefreshTokens.Add(entity);
            await _userManager.UpdateAsync(user);

            return new AuthDTO
            {
                Email = user.Email,
                ExpiresOn = jwtSecurityToken.ValidTo,
                IsAuthenticated = true,
                Roles = new List<string> { "User" },
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Username = user.UserName,
                RefreshToken = plain,
                RefreshTokenExpiration = entity.ExpiresOn
            };
        }

        public async Task<JwtSecurityToken> JwtSecurityTokenAsync(ApplicationUser User)
        {
            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, User.Id),
                new Claim(ClaimTypes.NameIdentifier, User.Id),
                new Claim(ClaimTypes.Name, User.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, User.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var userRoles = await _userManager.GetRolesAsync(User);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                claims: authClaims,
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(authKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)
            );

            return token;
        }

        public async Task<AuthDTO> LoginWithGoogleAsync(string idToken)
        {
            var googleUser = await _googleAuthService.VerifyTokenAsync(idToken);
            var user = await _userManager.FindByEmailAsync(googleUser.Email);
            if (user is null)
            {
                var spaceIndex = googleUser.Name.IndexOf(' ');
                user = new ApplicationUser()
                {
                    Email = googleUser.Email,
                    UserName = googleUser.Email,
                    FirstName = spaceIndex > 0 ? googleUser.Name.Substring(0, spaceIndex) : googleUser.Name,
                    LastName = spaceIndex > 0 ? googleUser.Name.Substring(spaceIndex + 1) : string.Empty,
                    LoginProvider = "Google",
                    ProviderKey = googleUser.ProviderKey,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Empty;
                    foreach (var error in result.Errors)
                        errors += $"{error.Description}, ";
                    return new AuthDTO() { Message = errors, IsAuthenticated = false };
                }
                await _userManager.AddToRoleAsync(user, "User");
            }
            var jwtSecurityToken = await JwtSecurityTokenAsync(user);

            var plain = GenerateRandomTokenPlain();
            var entity = CreateRefreshTokenEntity(plain, daysValid: 10);
            if (user.RefreshTokens == null)
                user.RefreshTokens = new List<RefreshToken>();
            user.RefreshTokens?.Add(entity);
            await _userManager.UpdateAsync(user);

            return new AuthDTO
            {
                Email = user.Email,
                ExpiresOn = jwtSecurityToken.ValidTo,
                IsAuthenticated = true,
                Roles = new List<string> { "User" },
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Username = user.UserName,
                RefreshToken = plain,
                RefreshTokenExpiration = entity.ExpiresOn
            };
        }

        public async Task<string> ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) throw new NotFoundException("User not found.");

            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var now = DateTime.UtcNow;
            var authCode = new UserCode
            {
                Code = OtpHasher.Hash(otpCode),
                ApplicationUserId = user.Id,
                CodeType = AuthCodeType.PasswordReset,
                CreatedOn = now,
                ExpiryDate = now.AddMinutes(15)
            };

            await _unitOfWork.ExecuteAsync(async () =>
            {
                // Rule 3: Set-based bulk update without loading tracked entities into memory
                await _userCodeRepo.GetQueryable()
                    .Where(u => u.ApplicationUserId == user.Id &&
                                u.CodeType == AuthCodeType.PasswordReset &&
                                !u.IsRevoked &&
                                !u.IsUsed &&
                                u.ExpiryDate > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRevoked, true));

                await _userCodeRepo.AddAsync(authCode);
            });

            var subject = "Travio - Reset Password Code";
            var emailBody = OTPEmailGenerator.GenerateEmailBody(user.FirstName ?? "Traveler", otpCode);
            await _emailSender.SendEmailAsync(user.Email, subject, emailBody);

            return "OTP code sent successfully to your email.";
        }

        public async Task<SendOtpResponseDto> SendEmailConfirmationAsync(SendOtpRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                throw new NotFoundException("Email Is Required");
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null) throw new NotFoundException("User not found.");

            var otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var now = DateTime.UtcNow;
            var authCode = new UserCode
            {
                Code = OtpHasher.Hash(otpCode),
                ApplicationUserId = user.Id,
                CodeType = AuthCodeType.EmailVerification,
                CreatedOn = now,
                ExpiryDate = now.AddMinutes(15)
            };

            await _unitOfWork.ExecuteAsync(async () =>
            {
                // Rule 3: Bulk update via ExecuteUpdateAsync
                await _userCodeRepo.GetQueryable()
                    .Where(u => u.ApplicationUserId == user.Id &&
                                u.CodeType == AuthCodeType.EmailVerification &&
                                !u.IsRevoked &&
                                !u.IsUsed &&
                                u.ExpiryDate > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRevoked, true));

                await _userCodeRepo.AddAsync(authCode);
            });

            var subject = "Travio - Confirm Email Code";
            var emailBody = OTPEmailGenerator.GenerateEmailBody(user.FirstName ?? "Traveler", otpCode);
            await _emailSender.SendEmailAsync(user.Email, subject, emailBody);
            return new SendOtpResponseDto(VerifyOtpStatus.Success, "OTP code sent successfully to your email.", authCode.ExpiryDate);
        }

        public async Task<VerifyOtpResponseDto> ConfirmEmailAsync(VerifyOtpRequestDto model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Otp))
            {
                throw new NotFoundException("Email and Code Is Required");
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }
            var result = await VerifyOtpAsync(user, model.Otp, AuthCodeType.EmailVerification);
            if (result?.status == VerifyOtpStatus.Success)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            return result;
        }

        public async Task<VerifyOtpResponseDto> VerifyOtpAsync(ApplicationUser user, string Otp, AuthCodeType CodeTyp)
        {
            if (user is null)
            {
                throw new NotFoundException("User not found.");
            }
            var hashedCode = OtpHasher.Hash(Otp);
            var now = DateTime.UtcNow;

            // Rule 6: Direct projection on GetQueryable()
            var codeInfo = await _userCodeRepo.GetQueryable()
                .AsNoTracking()
                .Where(c => c.ApplicationUserId == user.Id &&
                            !c.IsRevoked &&
                            !c.IsUsed &&
                            c.CodeType == CodeTyp &&
                            c.Code == hashedCode &&
                            c.ExpiryDate > now)
                .OrderByDescending(c => c.CreatedOn)
                .Select(c => new { c.Id, c.ExpiryDate })
                .FirstOrDefaultAsync();

            if (codeInfo is null)
            {
                return new VerifyOtpResponseDto(VerifyOtpStatus.CodeExpired, "Code is invalid! ");
            }
            if (codeInfo.ExpiryDate <= now)
            {
                return new VerifyOtpResponseDto(VerifyOtpStatus.CodeExpired, "The code is expired.");
            }

            // Rule 3: Atomic bulk update to mark code as revoked/used
            var rowsAffected = await _userCodeRepo.GetQueryable()
                .Where(c => c.Id == codeInfo.Id && !c.IsRevoked && !c.IsUsed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsRevoked, true)
                    .SetProperty(c => c.IsUsed, true));

            if (rowsAffected == 0)
            {
                return new VerifyOtpResponseDto(VerifyOtpStatus.CodeExpired, "Code has already been used.");
            }

            if (CodeTyp == AuthCodeType.PasswordReset)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                return new VerifyOtpResponseDto(VerifyOtpStatus.Success, "OTP verified successfully.", resetToken);
            }

            return new VerifyOtpResponseDto(VerifyOtpStatus.Success, "OTP verified successfully.");
        }

        private RefreshToken CreateRefreshTokenEntity(string tokenPlain, int daysValid = 10)
        {
            return new RefreshToken
            {
                TokenHash = HashToken(tokenPlain),
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(daysValid)
            };
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private string GenerateRandomTokenPlain()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public async Task DeleteOtps()
        {
            // Rule 3: Bulk delete expired OTPs directly in SQL without loading entities into memory
            await _userCodeRepo.GetQueryable()
                .Where(u => u.ExpiryDate < DateTime.UtcNow)
                .ExecuteDeleteAsync();
        }

        public async Task<ServiceResponse<string>> ResetPasswordAsync(ResetPasswordDTO Model)
        {
            var User = await _userManager.FindByEmailAsync(Model.Email);
            if (User == null) return new ServiceResponse<string>("Email is Invalid") { Success = false };
            var result = await _userManager.ResetPasswordAsync(User, Model.Token, Model.NewPassword);
            if (!result.Succeeded)
            {
                var Errors = result.Errors.Select(e => e.Description).ToList();
                return new ServiceResponse<string>("Password reset failed")
                {
                    Success = false,
                    Errors = Errors
                };
            }
            return new ServiceResponse<string>(data: "Password reset successfully", message: "Success");
        }
    }
}
