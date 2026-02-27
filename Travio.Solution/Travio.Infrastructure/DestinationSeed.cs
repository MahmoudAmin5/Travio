using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Infrastructure;

public class DestinationSeed
{
    private class DestinationSeedDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public List<string> ImageUrls { get; set; } = [];
        public List<string> Interests { get; set; } = [];
    }

    private class CitySeedDto
    {
        public int CityID { get; set; }
        public string CityName { get; set; } = null!;
        public List<DestinationSeedDto> Destinations { get; set; } = [];
    }

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Destinations.AnyAsync()) return;

        string jsonPath = @"D:\work\GraduationProject\Travio\Travio\Travio.Solution\Travio.Core\Data\all_cities_data.json";

        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"[Seed Warning] JSON file not found at: {jsonPath}");
            return;
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var cityData = JsonSerializer.Deserialize<List<CitySeedDto>>(jsonString, options);

            if (cityData is null) return;

            // Cache existing interests to avoid duplicates
            var interestCache = new Dictionary<string, Interest>(StringComparer.OrdinalIgnoreCase);

            foreach (var cityDto in cityData)
            {
                // Verify city exists in the database
                var cityExists = await context.Cities.AnyAsync(c => c.CityID == cityDto.CityID);
                if (!cityExists)
                {
                    Console.WriteLine($"[Seed Warning] City with ID {cityDto.CityID} ({cityDto.CityName}) not found. Skipping.");
                    continue;
                }

                foreach (var destDto in cityDto.Destinations)
                {
                    // Skip if destination already exists
                    var exists = await context.Destinations
                        .AnyAsync(d => d.Name == destDto.Name && d.CityID == cityDto.CityID);

                    if (exists) continue;

                    // 1. Create Destination
                    var destination = new Destination
                    {
                        CityID = cityDto.CityID,
                        Name = destDto.Name,
                        Description = destDto.Description,
                        Latitude = destDto.Latitude,
                        Longitude = destDto.Longitude,
                        Rating = destDto.Rating,
                        TotalReviews = destDto.TotalReviews
                    };

                    context.Destinations.Add(destination);
                    await context.SaveChangesAsync(); // Save to get DestinationID

                    // 2. Create DestinationImages
                    foreach (var imageUrl in destDto.ImageUrls)
                    {
                        context.Set<DestinationImage>().Add(new DestinationImage
                        {
                            DestinationID = destination.DestinationID,
                            ImageURL = imageUrl
                        });
                    }

                    // 3. Create Interests + DestinationInterest
                    foreach (var interestName in destDto.Interests)
                    {
                        if (!interestCache.TryGetValue(interestName, out var interest))
                        {
                            // Check DB first
                            interest = await context.Interests
                                .FirstOrDefaultAsync(i => i.InterestName == interestName);

                            if (interest is null)
                            {
                                interest = new Interest { InterestName = interestName };
                                context.Interests.Add(interest);
                                await context.SaveChangesAsync(); // Save to get InterestID
                            }

                            interestCache[interestName] = interest;
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

            Console.WriteLine("✅ Destination seeding completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during destination seeding: {ex.Message}");
        }
    }
}