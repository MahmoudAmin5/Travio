using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using Travio.Core.Domain.Entities.Hotelbeds;
using Travio.Core.Services.Hotelbeds.ApiModels;
using Travio.Infrastructure;

namespace Travio.Infrastructure.Jobs
{
    /// <summary>
    /// A background service that runs on startup (and optionally periodically)
    /// to synchronize static data (Destinations, Facilities) from Hotelbeds Content API
    /// to the local database for fast lookup and autocomplete.
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
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                // Use the Content API client which has HotelbedsAuthHandler attached
                var httpClient = httpClientFactory.CreateClient("HotelbedsContentApi");

                //await SyncDestinationsAsync(dbContext, httpClient, stoppingToken);
                //await SyncFacilitiesAsync(dbContext, httpClient, stoppingToken);

                _logger.LogInformation("HotelbedsStaticDataSyncJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Hotelbeds static data sync.");
            }
        }

        private async Task SyncDestinationsAsync(ApplicationDbContext dbContext, HttpClient httpClient, CancellationToken ct)
        {
            // 1. Define your primary markets for Travio (You can easily add more here)
            var targetCountries = new[]
 {
    // 🌍 Middle East & North Africa (MENA)
    "EG","AE", "SA", "QA", "BH", "KW", "OM", "MA", "TN", "JO", "LB",

    // 🇪🇺 Europe (Major Hubs & Mediterranean)
     "ES","GB", "FR", "IT", "DE", "GR", "PT", "CH", "AT", "NL",
    "BE", "SE", "NO", "FI", "DK", "IE", "PL", "CZ", "HU", "TR", "CY", "MT",

    // 🌎 The Americas & Caribbean
    "US", "CA", "MX", "BR", "AR", "CO", "CL", "PE",
    "DO", "JM", "BS", "CR", "PR", "AW",

    // 🌏 Asia & Pacific (Major Hubs & Tropical)
    "JP", "CN", "KR", "IN", "ID", "TH", "VN", "MY", "SG", "PH",
    "AU", "NZ", "MV", "LK", "FJ",

    // 🌍 Sub-Saharan Africa
    "ZA", "KE", "NG", "TZ", "MU", "SC"
};
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var countryCode in targetCountries)
            {
                _logger.LogInformation("Syncing Hotelbeds Destinations for Country: {CountryCode}...", countryCode);

                // 2. PERFORMANCE FIX: Pre-load existing destinations for this specific country into a dictionary.
                // This reduces 1,000+ SELECT queries down to exactly 1 query per country.
                var existingDestinations = await dbContext.HotelDestinations
                    .Where(d => d.CountryCode == countryCode)
                    .ToDictionaryAsync(d => d.Code, ct);

                int from = 1;
                int limit = 1000; // Hotelbeds max limit per request
                bool hasMoreData = true;
                int totalSyncedForCountry = 0;

                // 3. PAGINATION FIX: Loop until Hotelbeds stops returning full pages
                while (hasMoreData)
                {
                    int to = from + limit - 1;
                    var url = $"locations/destinations?countryCodes={countryCode}&fields=all&language=ENG&from={from}&to={to}";

                    var response = await httpClient.GetAsync(url, ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Failed to fetch destinations for {CountryCode}: {StatusCode}", countryCode, response.StatusCode);
                        break; // Break the while loop, move to the next country
                    }

                    var data = await response.Content.ReadFromJsonAsync<HotelbedsLocationsResponse>(options, ct);

                    // If the array is empty or null, we've reached the end of the cities for this country
                    if (data?.Destinations == null || data.Destinations.Count == 0)
                    {
                        hasMoreData = false;
                        break;
                    }

                    foreach (var apiDest in data.Destinations)
                    {
                        if (string.IsNullOrWhiteSpace(apiDest.Code)) continue;

                        // 4. Look up in memory (O(1) time complexity) instead of hitting SQL Server
                        if (existingDestinations.TryGetValue(apiDest.Code, out var existingDest))
                        {
                            // Update existing record
                            existingDest.Name = apiDest.Name?.Content ?? existingDest.Name;
                            existingDest.LastSyncedAt = DateTime.UtcNow;
                            dbContext.HotelDestinations.Update(existingDest);
                        }
                        else
                        {
                            // Insert new record
                            var newDest = new HotelDestination
                            {
                                Code = apiDest.Code,
                                Name = apiDest.Name?.Content ?? string.Empty,
                                CountryCode = apiDest.CountryCode ?? countryCode,
                                LastSyncedAt = DateTime.UtcNow
                            };

                            dbContext.HotelDestinations.Add(newDest);

                            // Add to our dictionary immediately so we don't throw an exception 
                            // if Hotelbeds returns duplicate codes in the same API response
                            existingDestinations.Add(newDest.Code, newDest);
                        }
                    }

                    totalSyncedForCountry += data.Destinations.Count;

                    // Check if we need to fetch another page
                    if (data.Destinations.Count < limit)
                    {
                        hasMoreData = false; // We got a partial page, meaning it's the last one
                    }
                    else
                    {
                        from += limit; // Increment our page counter for the next request
                    }
                }

                // 5. MEMORY OPTIMIZATION: Call SaveChanges after every country finishes.
                // This keeps EF Core's Change Tracker from bloating and using up all your server RAM.
                await dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Successfully synced {Count} destinations for {CountryCode}.", totalSyncedForCountry, countryCode);
            }
        }
        //private async Task SyncFacilitiesAsync(ApplicationDbContext dbContext, HttpClient httpClient, CancellationToken ct)
        //{
        //    _logger.LogInformation("Syncing Hotelbeds Facilities...");

        //    var response = await httpClient.GetAsync("types/facilities?fields=all&language=ENG&from=1&to=1000", ct);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        _logger.LogWarning("Failed to fetch facilities: {StatusCode}", response.StatusCode);
        //        return;
        //    }

        //    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        //    var data = await response.Content.ReadFromJsonAsync<HotelbedsFacilitiesResponse>(options, ct);

        //    if (data?.Facilities != null && data.Facilities.Count > 0)
        //    {
        //        var existingFacilities = await dbContext.HotelFacilities.ToListAsync(ct);

        //        foreach (var apiFac in data.Facilities)
        //        {
        //            var existing = existingFacilities.FirstOrDefault(f => f.FacilityCode == apiFac.Code && f.FacilityGroupCode == apiFac.FacilityGroupCode);
        //            if (existing == null)
        //            {
        //                dbContext.HotelFacilities.Add(new HotelFacility
        //                {
        //                    FacilityCode = apiFac.Code,
        //                    FacilityGroupCode = apiFac.FacilityGroupCode,
        //                    Description = apiFac.Description?.Content ?? string.Empty,
        //                    LastSyncedAt = DateTime.UtcNow
        //                });
        //            }
        //            else
        //            {
        //                existing.Description = apiFac.Description?.Content ?? existing.Description;
        //                existing.LastSyncedAt = DateTime.UtcNow;
        //                dbContext.HotelFacilities.Update(existing);
        //            }
        //        }

        //        await dbContext.SaveChangesAsync(ct);
        //        _logger.LogInformation("Synced {Count} facilities.", data.Facilities.Count);
        //    }
        //}
    }
}
