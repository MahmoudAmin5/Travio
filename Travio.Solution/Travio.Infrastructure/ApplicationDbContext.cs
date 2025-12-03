using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Infrastructure.Configrations;

namespace Travio.Infrastructure
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> Options) : base(Options) { }
        protected override void OnModelCreating(ModelBuilder builder) 
        {
            base.OnModelCreating(builder);

            // Apply all IEntityTypeConfiguration<T> from the assembly
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

           // configure identity relation tables (schema + names)
            IdentityTablesConfiguration.Configure(builder);
           
        }
        public DbSet<UserCode> UserCodes { get; set; }
    }
}
