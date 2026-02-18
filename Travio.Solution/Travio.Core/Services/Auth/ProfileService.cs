using Microsoft.AspNetCore.Http;
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

           
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return new ServiceResponse<UserProfileDTO> { Success = false, Errors = errors };
            }

            
            var updatedDto = new UserProfileDTO
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureURL
            };

            return new ServiceResponse<UserProfileDTO>(updatedDto, "Profile updated successfully");
        }

        public async Task<ServiceResponse<string>> UploadProfileImageAsync(string userId, IFormFile file)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new ServiceResponse<string>("User not found") { Success = false };

            if (file == null || file.Length == 0)
                return new ServiceResponse<string>("No image file provided") { Success = false };

            
            var fileName = $"{Guid.CreateVersion7()}{Path.GetExtension(file.FileName)}";

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var filePath = Path.Combine(uploadFolder, fileName);

            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            
            var fileUrl = $"/uploads/{fileName}";
            user.ProfilePictureURL = fileUrl;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return new ServiceResponse<string>("Failed to update profile picture") { Success = false };

            return new ServiceResponse<string>(data: fileUrl, message: "Image uploaded successfully");
        }
    }
}
