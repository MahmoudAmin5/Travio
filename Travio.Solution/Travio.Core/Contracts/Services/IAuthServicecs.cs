using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.DTOs;

namespace Travio.Core.Contracts.Services
{
    public interface IAuthService
    {
        Task<AuthDTO> RegisterAsync(RegisterDTO model);
        Task<AuthDTO> GetTokenAsync(LoginDTO model);
        Task<string> AddRoleAsync(AddRoleDTO model);
        Task<AuthDTO> RefreshTokenAsync(string token);
        Task<bool> RevokeTokenAsync(string token);
        Task<JwtSecurityToken> JwtSecurityTokenAsync(ApplicationUser User);
    }
}
