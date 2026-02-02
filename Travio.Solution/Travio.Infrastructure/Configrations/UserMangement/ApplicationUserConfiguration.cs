using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Infrastructure.Configrations.UserMangement
{
    internal class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            // Set schema and table name
            builder.ToTable("Users", "Account_Schema");
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(200);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(200);
            builder.HasIndex(u => u.Email).IsUnique();
        }
    }
}
