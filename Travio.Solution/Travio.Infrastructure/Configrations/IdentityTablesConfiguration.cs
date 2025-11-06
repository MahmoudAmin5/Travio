using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Travio.Infrastructure.Configrations
{
    internal class IdentityTablesConfiguration
    {
        public static void Configure(ModelBuilder builder)
        {
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "Account_Schema");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "Account_Schema");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "Account_Schema");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "Account_Schema");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "Account_Schema");
        }
    }
}
