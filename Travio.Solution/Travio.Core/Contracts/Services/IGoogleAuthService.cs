using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.DTOs;

namespace Travio.Core.Contracts.Services
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserDTO> VerifyTokenAsync(string idToken);
    }
}
