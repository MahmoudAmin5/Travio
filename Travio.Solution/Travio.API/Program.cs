using Serilog;
using Travio.API.Extensions;

namespace Travio.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting web host");
                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext());

                // Services
                builder.Services.AddControllers(options =>
                {
                    options.Filters.Add<Travio.API.Filters.EndpointLoggingFilter>();
                });
                builder.Services.AddDatabase(builder.Configuration);
                builder.Services.AddIdentityConfiguration();
                builder.Services.AddJwtAuthentication(builder.Configuration);
                builder.Services.AddApplicationServices(builder.Configuration);
                builder.Services.AddMapsterConfiguration();
                builder.Services.AddOpenApiConfiguration();
                builder.Services.AddHangfireConfiguration(builder.Configuration);

                // App
                var app = builder.Build();
                await app.ApplyMigrationsAndSeedAsync();
                app.ConfigureMiddleware();
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
