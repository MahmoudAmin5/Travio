namespace Travio.Core.Domain.Entities.TripPlaner;

public class SavedTripActivity
{
    public int Id { get; set; }
    public int SavedTripDayId { get; set; }
    public string? ActivityType { get; set; }
    public string? PlaceName { get; set; }
    public string? SuggestedTime { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? FeaturedImage { get; set; }

    // Navigation
    public SavedTripDay SavedTripDay { get; set; } = null!;
}
