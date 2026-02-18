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

        public async Task<ServiceResponse<UserProfileDTO>> UpdateProfileAsync(string userId, UpdateProfileDTO model)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return new ServiceResponse<UserProfileDTO>("User not found") { Success = false };

            // Update fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            // If you had the Country logic ready, you would update the ID here:
            // if (model.NationalityCountryId.HasValue) user.NationalityId = model.NationalityCountryId;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ServiceResponse<UserProfileDTO> { Success = false, Errors = errors };
            }

            // Return the updated data so the mobile app can refresh the screen immediately
            var updatedDto = new UserProfileDTO
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureURL
            };

            return new ServiceResponse<UserProfileDTO>(updatedDto, "Profile updated successfully");
        }
    }
}
