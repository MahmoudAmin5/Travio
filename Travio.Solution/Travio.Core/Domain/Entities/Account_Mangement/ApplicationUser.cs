using Microsoft.AspNetCore.Identity;
namespace Travio.Core.Domain.Entities.Account_Mangement
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set;  }
        public string LastName { get; set; }
        public string? ProfilePictureURL { get; set; } // optional 
        public DateTime RegistrationDate { get; set; }
        public string? LoginProvider { get; set; }
        public string? ProviderKey { get; set; }

        public List<RefreshToken>? RefreshTokens { get; set; }
    }
}
