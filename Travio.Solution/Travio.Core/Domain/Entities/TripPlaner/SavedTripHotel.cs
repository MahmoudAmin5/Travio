namespace Travio.Core.Domain.Entities.TripPlaner;

public class SavedTripHotel
{
    public int Id { get; set; }
    public int SavedTripId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public double? Rating { get; set; }
    public string? Address { get; set; }
    public string? Link { get; set; }
    public string? FeaturedImage { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Navigation
    public SavedTrip SavedTrip { get; set; } = null!;
}
