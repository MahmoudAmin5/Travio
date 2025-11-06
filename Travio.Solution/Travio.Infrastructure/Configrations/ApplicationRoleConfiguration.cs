using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Infrastructure.Configrations
{
    internal class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.ToTable("Roles", "Account_Schema");

            builder.Property(r => r.Description).HasMaxLength(500);
        }
    }
}
