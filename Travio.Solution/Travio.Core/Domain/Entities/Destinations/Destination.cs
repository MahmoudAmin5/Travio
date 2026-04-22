using System.ComponentModel.DataAnnotations.Schema;

namespace Travio.Core.Domain.Entities.Destinations;

public class Destination
{
    public int DestinationID { get; set; }
    public int CityID { get; set; } // FK
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public double Rating { get; set; }
    public int TotalReviews { get; set; }

    [Column(TypeName = "decimal(18, 10)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(18, 10)")]
    public decimal Longitude { get; set; }

    // Navigation
    public City City { get; set; } = null!;
    public ICollection<DestinationImage> Images { get; set; } = new HashSet<DestinationImage>();

    public ICollection<DestinationInterest> DestinationInterests { get; set; } = new HashSet<DestinationInterest>();

    public ICollection<DestinationReview> Reviews { get; set; } = new HashSet<DestinationReview>();

}
