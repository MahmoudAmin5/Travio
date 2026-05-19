using System.ComponentModel.DataAnnotations;

namespace Travio.Core.Domain.Entities.Hotelbeds
{
    /// <summary>
    /// Local lookup table to cache Hotelbeds destination data.
    /// This allows blazing fast autocomplete search without calling the Hotelbeds API.
    /// </summary>
    public class HotelDestination
    {
        /// <summary>The Hotelbeds 3-letter destination code (e.g., 'PMI').</summary>
        [Key]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        /// <summary>The name of the destination (e.g., 'Palma de Mallorca').</summary>
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        /// <summary>The ISO country code (e.g., 'ES', 'EG').</summary>
        [MaxLength(10)]
        public string CountryCode { get; set; } = string.Empty;
        
        /// <summary>Timestamp of the last sync from Hotelbeds API.</summary>
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    }
}
