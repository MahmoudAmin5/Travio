using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Helpers
{
    public static class HotelbedsAccommodationMapper
    {
        // Hotelbeds uses standard 3-letter codes for property types
        private static readonly Dictionary<string, string> _types = new(StringComparer.OrdinalIgnoreCase)
        {
            { "HTL",  "Hotel" },
            { "APT",  "Apartment" },
            { "APTH", "Aparthotel" },
            { "RST",  "Resort" },
            { "VLL",  "Villa" },
            { "HST",  "Hostel" },
            { "CMP",  "Camp" },
            { "B&B",  "Bed & Breakfast" },
            { "MOT",  "Motel" },
            { "LOD",  "Lodge" },
            { "BOU",  "Boutique Hotel" },
            { "GHS",  "Guest House" }
        };

        public static string GetName(string? code, string? fallbackDescription = null)
        {
            // 1. If we have a valid description from the API, trust it first
            if (!string.IsNullOrWhiteSpace(fallbackDescription) && fallbackDescription != "Unknown")
            {
                return fallbackDescription;
            }

            // 2. If we only have a code, look it up in our dictionary
            if (!string.IsNullOrWhiteSpace(code) && _types.TryGetValue(code, out var name))
            {
                return name;
            }

            // 3. Ultimate Fallback (90% of inventory are standard hotels)
            return "Hotel";
        }
    }
}
