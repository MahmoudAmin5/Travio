using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.Services.Hotelbeds.ApiModels;

namespace Travio.Infrastructure.Jobs
{
    /// <summary>
    /// A background service that runs on startup (and optionally periodically)
    /// to synchronize static data (Destinations, Facilities) from Hotelbeds Content API
    /// to the local database for fast lookup and autocomplete.
    /// Enforces Rule 6: Uses IGenericRepository and IUnitOfWork rather than ApplicationDbContext.
    /// </summary>
    public class HotelbedsStaticDataSyncJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HotelbedsStaticDataSyncJob> _logger;

        public HotelbedsStaticDataSyncJob(IServiceScopeFactory scopeFactory, ILogger<HotelbedsStaticDataSyncJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("HotelbedsStaticDataSyncJob starting.");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var destinationRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<HotelDestination>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                var httpClient = httpClientFactory.CreateClient("HotelbedsContentApi");

                // await SyncDestinationsAsync(destinationRepo, unitOfWork, httpClient, stoppingToken);

                _logger.LogInformation("HotelbedsStaticDataSyncJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Hotelbeds static data sync.");
            }
        }

        private async Task SyncDestinationsAsync(
            IGenericRepository<HotelDestination> destinationRepo,
            IUnitOfWork unitOfWork,
            HttpClient httpClient,
            CancellationToken ct)
        {
            var targetCountries = new[]
            {
                "EG","AE", "SA", "QA", "BH", "KW", "OM", "MA", "TN", "JO", "LB",
                "ES","GB", "FR", "IT", "DE", "GR", "PT", "CH", "AT", "NL",
                "BE", "SE", "NO", "FI", "DK", "IE", "PL", "CZ", "HU", "TR", "CY", "MT",
                "US", "CA", "MX", "BR", "AR", "CO", "CL", "PE",
                "DO", "JM", "BS", "CR", "PR", "AW",
                "JP", "CN", "KR", "IN", "ID", "TH", "VN", "MY", "SG", "PH",
                "AU", "NZ", "MV", "LK", "FJ",
                "ZA", "KE", "NG", "TZ", "MU", "SC"
            };
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var countryCode in targetCountries)
            {
                _logger.LogInformation("Syncing Hotelbeds Destinations for Country: {CountryCode}...", countryCode);

                var existingDestinations = await destinationRepo.GetQueryable()
                    .Where(d => d.CountryCode == countryCode)
                    .ToDictionaryAsync(d => d.Code, ct);

                int from = 1;
                int limit = 1000;
                bool hasMoreData = true;
                int totalSyncedForCountry = 0;

                await unitOfWork.ExecuteAsync(async () =>
                {
                    while (hasMoreData)
                    {
                        int to = from + limit - 1;
                        var url = $"locations/destinations?countryCodes={countryCode}&fields=all&language=ENG&from={from}&to={to}";

                        var response = await httpClient.GetAsync(url, ct);
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.LogWarning("Failed to fetch destinations for {CountryCode}: {StatusCode}", countryCode, response.StatusCode);
                            break;
                        }

                        var data = await response.Content.ReadFromJsonAsync<HotelbedsLocationsResponse>(options, ct);

                        if (data?.Destinations == null || data.Destinations.Count == 0)
                        {
                            hasMoreData = false;
                            break;
                        }

                        foreach (var apiDest in data.Destinations)
                        {
                            if (string.IsNullOrWhiteSpace(apiDest.Code)) continue;

                            if (existingDestinations.TryGetValue(apiDest.Code, out var existingDest))
                            {
                                existingDest.Name = apiDest.Name?.Content ?? existingDest.Name;
                                existingDest.LastSyncedAt = DateTime.UtcNow;
                                destinationRepo.Update(existingDest);
                            }
                            else
                            {
                                var newDest = new HotelDestination
                                {
                                    Code = apiDest.Code,
                                    Name = apiDest.Name?.Content ?? string.Empty,
                                    CountryCode = apiDest.CountryCode ?? countryCode,
                                    LastSyncedAt = DateTime.UtcNow
                                };

                                await destinationRepo.AddAsync(newDest, ct);
                                existingDestinations.Add(newDest.Code, newDest);
                            }
                        }

                        totalSyncedForCountry += data.Destinations.Count;

                        if (data.Destinations.Count < limit)
                        {
                            hasMoreData = false;
                        }
                        else
                        {
                            from += limit;
                        }
                    }
                }, ct);

                _logger.LogInformation("Successfully synced {Count} destinations for {CountryCode}.", totalSyncedForCountry, countryCode);
            }
        }
    }
}
