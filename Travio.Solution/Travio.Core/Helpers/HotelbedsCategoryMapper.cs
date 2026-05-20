using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Helpers
{
    public static class HotelbedsCategoryMapper
    {
        // A case-insensitive dictionary holding the common codes
        private static readonly Dictionary<string, string> _categories = new(StringComparer.OrdinalIgnoreCase)
        {
            { "1EST", "1 Star" },
            { "2EST", "2 Stars" },
            { "3EST", "3 Stars" },
            { "4EST", "4 Stars" },
            { "5EST", "5 Stars" },
            { "5LUX", "5 Stars Luxury" },
            { "4LUX", "4 Stars Luxury" },
            { "APTH", "Aparthotel" },
            { "HR",   "Hostel" },
            { "BB",   "Bed & Breakfast" },
            { "RS",   "Resort" },
            { "BOUT", "Boutique" },
            { "VILL", "Villa" },
            { "CAMP", "Camp" }
        };

        public static string GetCategoryName(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return "Unrated";

            // 1. Check if we know the exact code
            if (_categories.TryGetValue(code, out var name))
            {
                return name;
            }

            // 2. Smart Fallback: If it's a weird code we don't know, 
            // but it starts with a number (e.g., "3LL"), guess the star rating.
            if (char.IsDigit(code[0]))
            {
                return $"{code[0]} Stars";
            }

            // 3. Ultimate Fallback
            return "Standard";
        }
    }
}

