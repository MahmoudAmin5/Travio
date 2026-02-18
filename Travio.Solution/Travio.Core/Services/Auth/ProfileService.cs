using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.DTOs;
using Travio.Core.DTOs.ProfileDTOs;

namespace Travio.Core.Services.Auth
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<ServiceResponse<UserProfileDTO>> GetUserProfileAsync(string UserId)
        {
            var User = await _userManager.FindByIdAsync(UserId);
            if (User is null) return new ServiceResponse<UserProfileDTO>("User Not Found") { Success = false };
            var profile = new UserProfileDTO
            {
                Email = User.Email,
                FirstName = User.FirstName,
                LastName = User.LastName,
                ProfilePictureUrl = User.ProfilePictureURL
            };
            return new ServiceResponse<UserProfileDTO>(profile) { Success = true };
                
        }
    }
}
