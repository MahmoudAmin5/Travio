

using System.Text.Json;

namespace Travio.Infrastructure
{
    public class DestinationSeedDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public List<string> ImageUrls { get; set; }
        public List<string> Interests { get; set; }
    }

    public class DataGeneratorForDestenation
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "AIzaSyDuXEXbs9OuEaWzIijrUHIgNYHhOWusNRY";
        public DataGeneratorForDestenation()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<DestinationSeedDto>> FetchDestinationsForCityAsync(string cityName)
        {
            var destinations = new List<DestinationSeedDto>();
            string searchUrl = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query=top+tourist+attractions+in+{cityName}&key={_apiKey}";

            var searchResponse = await GetWithRetryAsync(searchUrl);
            using var searchDoc = JsonDocument.Parse(searchResponse);
            var results = searchDoc.RootElement.GetProperty("results");

            foreach (var place in results.EnumerateArray().Take(10))
            {
                string placeId = place.GetProperty("place_id").GetString();
                var details = await FetchPlaceDetailsAsync(placeId);

                if (details != null)
                {
                    destinations.Add(details);
                }

                await Task.Delay(200);
            }

            return destinations;
        }

        private async Task<DestinationSeedDto> FetchPlaceDetailsAsync(string placeId)
        {
            // أضفنا 'types' في حقل الـ fields لجلب التصنيفات
            string detailsUrl = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields=name,editorial_summary,rating,user_ratings_total,geometry,photo,types&key={_apiKey}";

            var response = await GetWithRetryAsync(detailsUrl);
            using var detailsDoc = JsonDocument.Parse(response);

            if (!detailsDoc.RootElement.TryGetProperty("result", out var result)) return null;

            string description = "اكتشف جمال هذا المكان السياحي المميز.";
            if (result.TryGetProperty("editorial_summary", out var summary))
            {
                description = summary.GetProperty("overview").GetString();
            }

            // سحب الاهتمامات (Types) من جوجل
            var interests = new List<string>();
            if (result.TryGetProperty("types", out var types))
            {
                foreach (var type in types.EnumerateArray())
                {
                    // تنظيف النص (إزالة الـ underscore وتحويله لحالة مناسبة)
                    string cleanType = type.GetString().Replace("_", " ");
                    interests.Add(cleanType);
                }
            }

            var images = new List<string>();
            if (result.TryGetProperty("photos", out var photos))
            {
                foreach (var photo in photos.EnumerateArray().Take(2))
                {
                    string photoRef = photo.GetProperty("photo_reference").GetString();
                    images.Add($"https://maps.googleapis.com/maps/api/place/photo?maxwidth=800&photoreference={photoRef}&key={_apiKey}");
                }
            }

            return new DestinationSeedDto
            {
                Name = result.GetProperty("name").GetString(),
                Description = description,
                Latitude = (decimal)result.GetProperty("geometry").GetProperty("location").GetProperty("lat").GetDouble(),
                Longitude = (decimal)result.GetProperty("geometry").GetProperty("location").GetProperty("lng").GetDouble(),
                Rating = result.TryGetProperty("rating", out var r) ? r.GetDouble() : 0,
                TotalReviews = result.TryGetProperty("user_ratings_total", out var t) ? t.GetInt32() : 0,
                ImageUrls = images,
                Interests = interests // حفظ قائمة الاهتمامات
            };
        }

        private async Task<string> GetWithRetryAsync(string url)
        {
            int retryCount = 0;
            while (retryCount < 5)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync();
                }
                catch { }
                await Task.Delay((int)Math.Pow(2, retryCount) * 1000);
                retryCount++;
            }
            throw new Exception("فشل الاتصال بـ Google API.");
        }
    }
}