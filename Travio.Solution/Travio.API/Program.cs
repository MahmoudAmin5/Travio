using Hangfire;
using HangfireBasicAuthenticationFilter;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text;
using Travio.API.Middleware;
using Travio.API.OpenApiTransformers;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Services.Auth;
using Travio.Core.Setting;
using Travio.Infrastructure;
using Travio.Infrastructure.Repositories;
namespace Travio.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddDbContext<ApplicationDbContext>(options => // add the ApplicationDbContext to the DI container
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")); // configure the context to use SQL Server with the connection string from appsettings.json
            });
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = builder.Configuration["JWTSetting:Issuer"],
                        ValidAudience = builder.Configuration["JWTSetting:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWTSetting:Key"])),
                        ClockSkew = TimeSpan.Zero
                    };
                });
            builder.Services.Configure<JWT>(builder.Configuration.GetSection("JWTSetting"));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped(typeof(IProfileService), typeof(ProfileService));
            builder.Services.AddTransient<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddTransient<IEmailSender, MailKitEmailSender>();
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

            });
            builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection")));
            var mappingConfiguration = TypeAdapterConfig.GlobalSettings;
            mappingConfiguration.Scan(Assembly.GetExecutingAssembly());
            builder.Services.AddSingleton<IMapper>(new Mapper(mappingConfiguration));
            #region DataSeeding and Apply Pending Migrations
            var app = builder.Build();
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // ask the clr to give me the instance of StoreContext
            var LoggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

            try
            {
                context.Database.Migrate(); // apply any pending migrations
                WorldCitiesSeed.SeedAsync(context).Wait(); // seed the database
                //var googleSeeder = new DataGeneratorForDestenation();
                //DestenationSeed.SeedAsync(context, googleSeeder).Wait();
                IdentitySeed.SeedRolesAndAdminAsync(app.Services, builder.Configuration).Wait();
            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "An error occurred during migration");
            }
            #endregion

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }
            app.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                DashboardTitle = "Travio Jobs",
                Authorization = [ new   HangfireCustomBasicAuthenticationFilter {
                        User = app.Configuration.GetValue<string>("Hangfire:user"),
                        Pass =  app.Configuration.GetValue<string>("Hangfire:pass")
                }]
            });
            var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
            using var scopee = scopeFactory.CreateScope();
            var authService = scopee.ServiceProvider.GetRequiredService<IAuthService>();

            RecurringJob.AddOrUpdate("DeleteOtpAuthService", () => authService.DeleteOtps(), Cron.Daily);
            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
            // test 
        }
    }
}
