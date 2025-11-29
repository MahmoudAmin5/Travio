using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services;
using Travio.Core.DTOs;

namespace Travio.Core.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _confguration;

        public GoogleAuthService(IConfiguration confguration)
        {
            _confguration = confguration;
        }
        public async Task<GoogleUserDTO> VerifyTokenAsync(string idToken)
        {
            try
            {
                var clientId = _confguration["Google:ClientId"];
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new List<string> { clientId }
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return new GoogleUserDTO
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    ProviderKey = payload.Subject,
                    PictureURL = payload.Picture,

                };
            }
            catch (InvalidJwtException ex) 
            {
                throw new ValidationException($"Invalid Google Token: {ex.Message}");
            }
        }
    }
}
