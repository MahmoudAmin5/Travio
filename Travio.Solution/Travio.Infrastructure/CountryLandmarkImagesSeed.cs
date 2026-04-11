using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Travio.Infrastructure;

public static class CountryLandmarkImagesSeed
{
    private static readonly Dictionary<string, string> CountryNameAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["unitedarabemirates"] = "uae",
        ["unitedstates"] = "usa",
        ["unitedstatesofamerica"] = "usa",
        ["republicofkorea"] = "southkorea",
        ["koreasouth"] = "southkorea"
    };

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        var imagesFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Landmarks_Images");

        if (!Directory.Exists(imagesFolderPath))
        {
            Console.WriteLine($"[Seed Warning] Landmarks images folder not found at: {imagesFolderPath}");
            return;
        }

        var files = Directory.GetFiles(imagesFolderPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        if (!files.Any())
        {
            Console.WriteLine("[Seed Warning] No landmark images found.");
            return;
        }

        var fileLookup = files.ToDictionary(
            keySelector: fileName => NormalizeKey(Path.GetFileNameWithoutExtension(fileName)),
            elementSelector: fileName => fileName,
            comparer: StringComparer.OrdinalIgnoreCase);

        var countries = await context.Countries.ToListAsync();
        var updatedCount = 0;

        foreach (var country in countries)
        {
            var normalizedCountryName = NormalizeKey(country.Name);
            var lookupKey = CountryNameAliases.GetValueOrDefault(normalizedCountryName, normalizedCountryName);

            if (!fileLookup.TryGetValue(lookupKey, out var fileName))
            {
                continue;
            }

            var imageUrl = $"/Landmarks_Images/{Uri.EscapeDataString(fileName)}";

            if (string.Equals(country.ImageURL, imageUrl, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            country.ImageURL = imageUrl;
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync();
        }

        Console.WriteLine($"✅ Country landmark images seed finished. Updated countries: {updatedCount}");
    }

    private static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }
}
