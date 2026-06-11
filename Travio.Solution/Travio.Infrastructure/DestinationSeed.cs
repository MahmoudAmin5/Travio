using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Infrastructure;

public class DestinationSeed
{
    private class TravioCountryDto
    {
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
        [JsonPropertyName("cities")]
        public List<TravioCityDto> Cities { get; set; } = new();
    }

    private class TravioCityDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("landmarks")]
        public List<TravioLandmarkDto> Landmarks { get; set; } = new();
    }

    private class TravioLandmarkDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("coordinates")]
        public TravioCoordinatesDto Coordinates { get; set; } = new();
        [JsonPropertyName("categories")]
        public List<string> Categories { get; set; } = new();
        [JsonPropertyName("image_urls")]
        public List<string> ImageUrls { get; set; } = new();
    }

    private class TravioCoordinatesDto
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Destinations.AnyAsync()) return;

        // Ensure we load the file correctly relative to the project where it runs
        string jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "travio_final_with_urls.json");
        
        if (!File.Exists(jsonPath))
        {
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Travio.Core", "Data", "travio_final_with_urls.json");
        }

        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"[Seed Warning] JSON file not found at: {jsonPath}");
            return;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var countryData = JsonSerializer.Deserialize<List<TravioCountryDto>>(jsonString, options);

            if (countryData == null) return;

            var interestCache = new Dictionary<string, Interest>(StringComparer.OrdinalIgnoreCase);

            var dbCities = await context.Cities
                .Include(c => c.Country)
                .ToListAsync();

            var random = new Random();

            foreach (var countryDto in countryData)
            {
                foreach (var cityDto in countryDto.Cities)
                {
                    var dbCity = dbCities.FirstOrDefault(c => 
                        string.Equals(c.Name, cityDto.Name, StringComparison.OrdinalIgnoreCase) && 
                        c.Country != null && 
                        string.Equals(c.Country.Name, countryDto.Country, StringComparison.OrdinalIgnoreCase));

                    if (dbCity == null)
                    {
                        Console.WriteLine($"[Seed Warning] City '{cityDto.Name}' in '{countryDto.Country}' not found in DB. Skipping.");
                        continue;
                    }

                    foreach (var landmarkDto in cityDto.Landmarks)
                    {
                        var destination = new Destination
                        {
                            CityID = dbCity.CityID,
                            Name = landmarkDto.Name,
                            Description = landmarkDto.Description,
                            Latitude = (decimal)landmarkDto.Coordinates.Latitude,
                            Longitude = (decimal)landmarkDto.Coordinates.Longitude,
                            Rating = Math.Round(4.0 + (random.NextDouble() * 1.0), 1),
                            TotalReviews = random.Next(100, 5000)
                        };

                        context.Destinations.Add(destination);
                        await context.SaveChangesAsync();

                        foreach (var imgUrl in landmarkDto.ImageUrls)
                        {
                            context.Set<DestinationImage>().Add(new DestinationImage
                            {
                                DestinationID = destination.DestinationID,
                                ImageURL = imgUrl
                            });
                        }

                        foreach (var categoryName in landmarkDto.Categories)
                        {
                            if (!interestCache.TryGetValue(categoryName, out var interest))
                            {
                                interest = await context.Interests
                                    .FirstOrDefaultAsync(i => i.InterestName.ToLower() == categoryName.ToLower());

                                if (interest == null)
                                {
                                    interest = new Interest { InterestName = categoryName };
                                    context.Interests.Add(interest);
                                    await context.SaveChangesAsync();
                                }
                                interestCache[categoryName] = interest;
                            }

                            context.Set<DestinationInterest>().Add(new DestinationInterest
                            {
                                DestinationID = destination.DestinationID,
                                InterestID = interest.InterestID
                            });
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }

            Console.WriteLine("✅ Database Seeding for Destinations completed successfully from travio_final_with_urls.json.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during destination seeding: {ex.Message}");
        }
    }
}
// End of file