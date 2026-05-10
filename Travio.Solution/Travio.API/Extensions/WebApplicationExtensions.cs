using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Travio.API.Middleware;
using Travio.Core.Contracts.Services.Auth;
using Travio.Infrastructure;

namespace Travio.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        try
        {
            if (app.Environment.IsDevelopment())
            {
                await context.Database.MigrateAsync();
            }

            await WorldCitiesSeed.SeedAsync(context);
            await CountryLandmarkImagesSeed.SeedAsync(context);
            await DestinationSeed.SeedAsync(context);
            await IdentitySeed.SeedRolesAndAdminAsync(app.Services, app.Configuration);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "An error occurred during migration/seeding");
        }
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
        //app.UseRateLimiter();
        app.UseStaticFiles();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static WebApplication ConfigureHangfire(this WebApplication app)
    {
        app.UseHangfireDashboard("/jobs", new DashboardOptions
        {
            DashboardTitle = "Travio Jobs",
            Authorization = [new HangfireCustomBasicAuthenticationFilter
            {
                User = app.Configuration.GetValue<string>("Hangfire:user"),
                Pass = app.Configuration.GetValue<string>("Hangfire:pass")
            }]
        });

        using var scope = app.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        RecurringJob.AddOrUpdate("DeleteOtpAuthService", () => authService.DeleteOtps(), Cron.Daily);

        return app;
    }
}