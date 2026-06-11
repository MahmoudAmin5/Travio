using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Travio.Core.Domain.Entities.Destinations;

namespace Travio.Infrastructure;

public class WorldCitiesSeed
{
    // DTOs to match the world_cities.json structure
    private class CitySeedDto { public string CityName { get; set; } }
    private class CountrySeedDto
    {
        public string CountryName { get; set; }
        public string FlagURL { get; set; }
        public List<CitySeedDto> Cities { get; set; }
    }
    private class ContinentSeedDto
    {
        public string ContinentName { get; set; }
        public List<CountrySeedDto> Countries { get; set; }
    }

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // 1. Path to your JSON file (ensure the file is in the root or set to 'Copy to Output Directory')
        string jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "WorldCities.json");
        
        if (!File.Exists(jsonPath))
        {
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Travio.Core", "Data", "WorldCities.json");
        }

        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"[Seed Warning] JSON file not found at: {jsonPath}");
            return;
        }

        try
        {
            // 2. Read and Deserialize JSON
            var jsonString = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var continentData = JsonSerializer.Deserialize<List<ContinentSeedDto>>(jsonString, options);

            if (continentData == null) return;

            foreach (var contDto in continentData)
            {
                // 3. Process Continent
                var continent = await context.Continents
                    .FirstOrDefaultAsync(c => c.Name == contDto.ContinentName);

                if (continent == null)
                {
                    continent = new Continent { Name = contDto.ContinentName };
                    context.Continents.Add(continent);
                    await context.SaveChangesAsync(); // Save to get ContinentID
                }

                foreach (var countryDto in contDto.Countries)
                {
                    // 4. Process Country
                    var country = await context.Countries
                        .FirstOrDefaultAsync(c => c.Name == countryDto.CountryName && c.ContinentID == continent.ContinentID);

                    if (country == null)
                    {
                        country = new Country
                        {
                            Name = countryDto.CountryName,
                            FlagURL = countryDto.FlagURL,
                            ContinentID = continent.ContinentID
                        };
                        context.Countries.Add(country);
                        await context.SaveChangesAsync(); // Save to get CountryID
                    }

                    // 5. Process Cities
                    foreach (var cityDto in countryDto.Cities)
                    {
                        var cityExists = await context.Cities
                            .AnyAsync(c => c.Name == cityDto.CityName && c.CountryID == country.CountryID);

                        if (!cityExists)
                        {
                            context.Cities.Add(new City
                            {
                                Name = cityDto.CityName,
                                CountryID = country.CountryID
                            });
                        }
                    }
                }
                // Save cities for the current continent
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Database Seeding for World Cities completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during seeding: {ex.Message}");
        }
    }
}
