namespace Travio.Core.Domain.Entities.Destinations;

public class DestinationImage
{
    public int DestinationImageID { get; set; }
    public int DestinationID { get; set; } // FK
    public string ImageURL { get; set; } = null!;
    // Navigation
    public Destination Destination { get; set; } = null!;
}
