using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.GeocodingService;

namespace Travio.Core.Services.Shared.GeocodingService
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NominatimGeocodingService> _logger;

        public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            // CRITICAL: Nominatim will block your IP if you don't set a User-Agent
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "TravioApp/1.0 (contact@travio.com)");
            }
        }

        public async Task<(decimal Lat, decimal Lng)?> GetCoordinatesAsync(string address, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(address)) return null;

            try
            {
                // Format the URL. We request JSON and limit to 1 result for speed.
                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

                var results = await _httpClient.GetFromJsonAsync<List<NominatimResponse>>(url, ct);

                if (results != null && results.Count > 0)
                {
                    var match = results[0];

                    // Nominatim returns coordinates as strings, so we must parse them
                    if (decimal.TryParse(match.Lat, out decimal lat) && decimal.TryParse(match.Lon, out decimal lng))
                    {
                        return (lat, lng);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geocoding failed for address: {Address}", address);
            }

            return null; // Location not found or API failed
        }

        // Private class to deserialize the OpenStreetMap JSON response
        private class NominatimResponse
        {
            [JsonPropertyName("lat")]
            public string Lat { get; set; } = string.Empty;

            [JsonPropertyName("lon")]
            public string Lon { get; set; } = string.Empty;
        }
    }
}
