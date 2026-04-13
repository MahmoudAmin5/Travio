using Microsoft.AspNetCore.Identity;
using Travio.Core.Domain.Entities.Destinations;
namespace Travio.Core.Domain.Entities.Account_Mangement;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureURL { get; set; } // optional 
    public DateTime RegistrationDate { get; set; }
    public string? LoginProvider { get; set; }
    public string? ProviderKey { get; set; }

    public List<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserPreference> UserPreferences { get; set; } = new HashSet<UserPreference>();
}
