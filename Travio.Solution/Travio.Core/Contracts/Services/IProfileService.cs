using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs;
using Travio.Core.DTOs.ProfileDTOs;

namespace Travio.Core.Contracts.Services
{
    public interface IProfileService
    {
        Task<ServiceResponse<UserProfileDTO>> GetUserProfileAsync(string UserId);
        Task<ServiceResponse<UserProfileDTO>> UpdateProfileAsync(string userId, UpdateProfileDTO model);
    }
}
