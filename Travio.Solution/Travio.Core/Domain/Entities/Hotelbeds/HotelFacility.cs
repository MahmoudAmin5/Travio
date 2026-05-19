using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Travio.Core.Domain.Entities.Hotelbeds
{
    /// <summary>
    /// Local lookup table to cache Hotelbeds facilities data.
    /// Used to resolve facility codes to human-readable names locally.
    /// </summary>
    public class HotelFacility
    {
        /// <summary>Internal DB primary key.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>The Hotelbeds facility code.</summary>
        public int FacilityCode { get; set; }

        /// <summary>The Hotelbeds facility group code.</summary>
        public int FacilityGroupCode { get; set; }

        /// <summary>The human-readable description of the facility.</summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>Timestamp of the last sync from Hotelbeds API.</summary>
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
    }
}
