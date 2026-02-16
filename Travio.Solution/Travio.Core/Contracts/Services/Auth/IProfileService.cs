using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs;
using Travio.Core.DTOs.ProfileDTOs;

namespace Travio.Core.Contracts.Services.Auth
{
    public interface IProfileService
    {
        Task<ServiceResponse<UserProfileDTO>> GetUserProfileAsync(string UserId);
    }
}
