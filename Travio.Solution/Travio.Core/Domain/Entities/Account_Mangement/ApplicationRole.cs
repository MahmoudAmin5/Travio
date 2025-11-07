using Microsoft.AspNetCore.Identity;

namespace Travio.Core.Domain.Entities.Account_Mangement
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
