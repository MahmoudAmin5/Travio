using Hangfire;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Travio.API.Hubs;
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
            await ReviewSeed.SeedAsync(app.Services);
        }
        catch (Exception ex)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogError(ex, "An error occurred during migration/seeding");
            Console.WriteLine($"❌ [Seed] Error during migration/seeding: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"❌ [Seed] Inner: {ex.InnerException.Message}");
        }
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSerilogRequestLogging();

        app.MapOpenApi();
        app.MapScalarApiReference();
        //app.UseRateLimiter();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<TripPlanerHub>("/hubs/trip-planer");

        return app;
    }

    public static WebApplication ConfigureHangfire(this WebApplication app)
    {
        app.UseHangfireDashboard("/jobs", new DashboardOptions
        {
            DashboardTitle = "Travio Jobs",
            Authorization = app.Environment.IsDevelopment()
                ? [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
                : [new Filters.HangfireDashboardAuthFilter(
                    app.Configuration.GetValue<string>("Hangfire:user")!,
                    app.Configuration.GetValue<string>("Hangfire:pass")!
                )]
        });

        RecurringJob.AddOrUpdate<IAuthService>("DeleteOtpAuthService", service => service.DeleteOtps(), Cron.Daily);

        return app;
    }
}