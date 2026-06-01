using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Contracts.Services.CurruncyExchange;

namespace Travio.Core.Services.Shared.CurrencyExchange
{
    public class CurrencyExchangeService : ICurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CurrencyExchangeService> _logger;

        public CurrencyExchangeService(HttpClient httpClient, IMemoryCache cache, ILogger<CurrencyExchangeService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<decimal> GetExchangeRateAsync(string baseCurrency, string targetCurrency, CancellationToken ct = default)
        {
            baseCurrency = baseCurrency.ToUpper();
            targetCurrency = targetCurrency.ToUpper();

            // If they match, the multiplier is exactly 1
            if (baseCurrency == targetCurrency) return 1.0m;

            string cacheKey = $"ExchangeRates_{baseCurrency}";

            // 1. Check if the entire dictionary of rates is in RAM
            if (!_cache.TryGetValue(cacheKey, out Dictionary<string, decimal>? rates))
            {
                try
                {
                    // 2. Fetch from the free Open Exchange Rates API
                    var url = $"https://open.er-api.com/v6/latest/{baseCurrency}";
                    var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(url, ct);

                    if (response?.Rates != null)
                    {
                        // Convert double to decimal for financial safety
                        rates = response.Rates.ToDictionary(k => k.Key, v => (decimal)v.Value);

                        // 3. Cache it for 12 hours
                        _cache.Set(cacheKey, rates, TimeSpan.FromHours(12));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch live exchange rates for base currency {BaseCurrency}.", baseCurrency);
                }
            }

            // 4. Extract the specific currency the user requested
            if (rates != null && rates.TryGetValue(targetCurrency, out decimal targetRate))
            {
                return targetRate;
            }

            // 5. Safe Fallback: If everything fails, return 1.0 to prevent crashing the booking flow.
            _logger.LogWarning("Falling back to 1.0 exchange rate. Could not resolve {Base} to {Target}", baseCurrency, targetCurrency);
            return 1.0m;
        }

        // Internal record specifically for deserializing the third-party API
        private record ExchangeRateResponse(Dictionary<string, double> Rates);
    }

}

