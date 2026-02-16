using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Travio.Infrastructure;

public class DestenationSeed
{

    public static async Task SeedAsync(ApplicationDbContext _context, DataGeneratorForDestenation _googleSeeder)
    {
        Console.WriteLine(" Starting data fetching process for all cities from the database...");

        // Fetch all cities from the database
        var cities = await _context.Cities
            .Include(c => c.Country)
            .ToListAsync();

        if (!cities.Any())
        {
            Console.WriteLine(" No cities found in the database.");
            return;
        }

        Console.WriteLine($" Found {cities.Count} cities to process.");

        // List to aggregate results
        var masterDataList = new List<object>();

        foreach (var city in cities)
        {
            Console.WriteLine($" Fetching destinations for: {city.Name}, {city.Country?.Name}...");

            try
            {
                var destinations = await _googleSeeder.FetchDestinationsForCityAsync(city.Name);

                if (destinations != null && destinations.Any())
                {
                    masterDataList.Add(new
                    {
                        CityID = city.CityID,
                        CityName = city.Name,
                        CountryName = city.Country?.Name,
                        TotalFetched = destinations.Count,
                        Destinations = destinations
                    });

                    Console.WriteLine($" Successfully fetched {destinations.Count} destinations for {city.Name}.");
                }
                else
                {
                    Console.WriteLine($"info: No destinations found for {city.Name}.");
                }

                // Small delay to respect API quota
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error while processing {city.Name}: {ex.Message}", WarningBehavior.Log);
            }
        }

        // Save everything to a single file
        if (masterDataList.Any())
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "all_cities_data.json");
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string finalJson = JsonSerializer.Serialize(masterDataList, options);
            await File.WriteAllTextAsync(filePath, finalJson);

            Console.WriteLine($"\n⭐ Process completed successfully! All data saved to: {filePath}");
        }
        else
        {
            Console.WriteLine("\n Process finished but no data was collected.", WarningBehavior.Log);
        }
    }
}
