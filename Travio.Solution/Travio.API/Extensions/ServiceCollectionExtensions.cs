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
using System.Threading.RateLimiting;
using Travio.API.OpenApiTransformers;
using Travio.Core.Contracts.Services.Auth;
using Travio.Core.Contracts.Services.Community;
using Travio.Core.Contracts.Services.CurruncyExchange;
using Travio.Core.Contracts.Services.Destination;
using Travio.Core.Contracts.Services.DuffelFlights;
using Travio.Core.Contracts.Services.DuffelHotels;
using Travio.Core.Contracts.Services.GeocodingService;
using Travio.Core.Contracts.Services.Hotelbeds;
using Travio.Core.Contracts.Services.Payment;
using Travio.Core.Contracts.Services.Survey;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.Domain.Entities.Account_Mangement;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Services.Auth;
using Travio.Core.Services.Community;
using Travio.Core.Services.Destinations;
using Travio.Core.Services.DuffelFlights;
using Travio.Core.Services.DuffelHotels;
using Travio.Core.Services.Hotelbeds;
using Travio.Core.Services.Payment;
using Travio.Core.Services.Shared.CurrencyExchange;
using Travio.Core.Services.Shared.GeocodingService;
using Travio.Core.Services.Survey;
using Travio.Core.Services.TripPlaner;
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

            // Allow SignalR to receive the JWT token from the query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
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
        services.AddScoped<ISavedTripService, SavedTripService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddTransient<IGoogleAuthService, GoogleAuthService>();
        services.AddTransient<IEmailSender, MailKitEmailSender>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();

        var duffelToken = configuration["Duffel:AccessToken"];
        services.AddHttpClient<IDuffelFlightBookingService, DuffelFlightBookingService>(client =>
        {
            client.BaseAddress = new Uri("https://api.duffel.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", duffelToken);

          
            client.DefaultRequestHeaders.Add("Duffel-Version", "v2");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddHttpClient<IDuffelHotelsService, DuffelHotelsService>(client =>
        {
            client.BaseAddress = new Uri("https://api.duffel.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", duffelToken);

            
            client.DefaultRequestHeaders.Add("Duffel-Version", "v2");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
        services.AddHttpClient<ITripPlanerService, TripPlanerService>(client =>
        {
            client.BaseAddress = new Uri("http://127.0.0.1:8000/");
            client.Timeout = TimeSpan.FromMinutes(3);
        });
       services.AddMemoryCache();
        services.AddHttpClient<ICurrencyExchangeService, CurrencyExchangeService>();
        services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();
        services.AddRateLimiter(options =>
        {
          
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

          
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    "{\"success\": false, \"message\": \"Too many requests. Please slow down.\"}", token);
            };

         
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
              
                var userIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: userIp,
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });
        var stripeKey = configuration["Stripe:SecretKey"];
        Stripe.StripeConfiguration.ApiKey = stripeKey;

        services.AddValidatorsFromAssembly(typeof(CreatePostValidator).Assembly);
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true; // Shows real exception messages in dev
        });

        // ── Hotelbeds APITUDE API Integration ──────────────────────────────
        // 0. Add in-memory cache for Content API responses (images/descriptions rarely change)
        services.AddMemoryCache();

        // Register Background Job for Static Data Sync
        services.AddHostedService<Travio.Infrastructure.Jobs.HotelbedsStaticDataSyncJob>();

        // 1. Bind HotelbedsSettings from appsettings.json via the Options Pattern
        services.Configure<HotelbedsSettings>(configuration.GetSection("HotelbedsSettings"));

        // 2. Register the custom auth handler as transient (new instance per request)
        services.AddTransient<HotelbedsAuthHandler>();

        // 3. Register the typed HttpClient with base URL and the auth handler attached.
        //    The HotelbedsAuthHandler intercepts every request to inject Api-key and X-Signature headers.
        services.AddHttpClient<IHotelbedsService, HotelbedsService>(client =>
        {
            var baseUrl = configuration["HotelbedsSettings:BaseUrl"]
                ?? "https://api.test.hotelbeds.com/hotel-api/1.0/";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddHttpMessageHandler<HotelbedsAuthHandler>();

        services.AddHttpClient("HotelbedsContentApi", client =>
        {
            var baseUrl = configuration["HotelbedsSettings:ContentApiBaseUrl"]
                ?? "https://api.test.hotelbeds.com/hotel-content-api/1.0/";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .AddHttpMessageHandler<HotelbedsAuthHandler>();

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