using Travio.API.Extensions;

namespace Travio.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Services
            builder.Services.AddControllers();
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
            app.ConfigureHangfire();
            app.Run();
        }
    }
}
