using FluentValidation;
using Hangfire;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Travio.API.OpenApiTransformers;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.DuffelHotels;
using Travio.Core.Contracts.Services.Survey;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Services.Auth;
using Travio.Core.Services.Community;
using Travio.Core.Services.Destinations;
using Travio.Core.Services.DuffelFlights;
using Travio.Core.Services.DuffelHotels;
using Travio.Core.Services.Survey;
using Travio.Core.Setting;
using Travio.Core.Validators;
using Travio.Infrastructure;
using Travio.Infrastructure.Repositories;

namespace Travio.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .LogTo(log => Debug.WriteLine(log), LogLevel.Information));

        return services;
    }

    public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JWT>(configuration.GetSection("JWTSetting"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = configuration["JWTSetting:Issuer"],
                ValidAudience = configuration["JWTSetting:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["JWTSetting:Key"]
                        ?? throw new InvalidOperationException("JWT Key is not configured."))),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        // Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDestinationService, DestinationService>();
        services.AddScoped<ICommunityService, CommunityService>();
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<IUserFavoriteService, UserFavoriteService>();
        services.AddTransient<IGoogleAuthService, GoogleAuthService>();
        services.AddTransient<IEmailSender, MailKitEmailSender>();
        var duffelToken = configuration["Duffel:AccessToken"];
        services.AddHttpClient<IDuffelFlightBookingService, DuffelFlightBookingService>(client =>
        {
            client.BaseAddress = new Uri("https://api.duffel.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", duffelToken);

            // THIS IS THE MAGIC FIX: Forcing the modern V2 API version!
            client.DefaultRequestHeaders.Add("Duffel-Version", "v2");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddHttpClient<IDuffelHotelsService, DuffelHotelsService>(client =>
        {
            client.BaseAddress = new Uri("https://api.duffel.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", duffelToken);

            // THIS IS THE MAGIC FIX: Forcing the modern V2 API version!
            client.DefaultRequestHeaders.Add("Duffel-Version", "v2");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });


        // Validators
        services.AddValidatorsFromAssembly(typeof(CreatePostValidator).Assembly);

        return services;
    }

    public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(Travio.Core.Services.Destinations.DestinationService).Assembly);
        services.AddSingleton<IMapper>(new Mapper(config));

        return services;
    }

    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection")));

        services.AddHangfireServer();

        return services;
    }

    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        return services;
    }
}