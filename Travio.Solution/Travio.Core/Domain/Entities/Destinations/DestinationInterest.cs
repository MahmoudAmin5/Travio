namespace Travio.Core.Domain.Entities.Destinations;

public class DestinationInterest
{
    public int DestinationInterestID { get; set; }
    public int DestinationID { get; set; } // FK
    public int InterestID { get; set; } // FK
    // Navigation
    public Destination Destination { get; set; } = null!;
    public Interest Interest { get; set; } = null!;
}
