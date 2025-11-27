using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Infrastructure
{
    public static class IdentitySeed
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            // Serilog will be added here soon 


            // roles to ensure exist
            var roles = new[]
            {
                new ApplicationRole { Name = "Admin", Description = "Site administrators" },
                new ApplicationRole { Name = "User", Description = "Regular users" }
            };
            foreach (var role in roles)
            {
                if (!await roleMgr.RoleExistsAsync(role.Name))
                {
                    var result = await roleMgr.CreateAsync(role);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));

                        throw new Exception($"Failed to create role {role.Name}: {errors}");
                    }
                }
            }


            // create admin user if not exists (credentials from config or env)
            var adminEmail = configuration["Seed:AdminEmail"];
            var adminUserName = configuration["Seed:AdminUserName"];
            var adminPassword = configuration["Seed:AdminPassword"];

            var adminUser = await userMgr.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "System",
                    LastName = "Admin",
                };

                var createRes = await userMgr.CreateAsync(adminUser, adminPassword);
                if (!createRes.Succeeded)
                {
                    var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                    // Serilog will be added here soon 
                    throw new Exception($"Failed to create admin user: {errors}");
                }
            }

            // ensure admin is in Admin role
            if (!await userMgr.IsInRoleAsync(adminUser, "Admin"))
            {
                var addRoleRes = await userMgr.AddToRoleAsync(adminUser, "Admin");
                if (!addRoleRes.Succeeded)
                {
                    var errors = string.Join(", ", addRoleRes.Errors.Select(e => e.Description));
                    // Serilog will be added here soon 
                    throw new Exception($"Failed to add admin user to Admin role: {errors}");
                }
            }
        }
    }
}
