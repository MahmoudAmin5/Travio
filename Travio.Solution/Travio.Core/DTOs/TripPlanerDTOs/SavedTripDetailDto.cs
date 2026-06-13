namespace Travio.Core.DTOs.TripPlanerDTOs;

public class SavedTripDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? DestinationName { get; set; }
    public string? CityHeroImage { get; set; }
    public int TotalDays { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<SavedTripDayDto> Days { get; set; } = new();
    public List<SavedTripHotelDto> Hotels { get; set; } = new();
}

public class SavedTripDayDto
{
    public int DayNumber { get; set; }
    public string? Theme { get; set; }
    public List<SavedTripActivityDto> Activities { get; set; } = new();
}

public class SavedTripActivityDto
{
    public string? ActivityType { get; set; }
    public string? PlaceName { get; set; }
    public string? SuggestedTime { get; set; }
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? FeaturedImage { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class SavedTripHotelDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public double? Rating { get; set; }
    public string? Address { get; set; }
    public string? Link { get; set; }
    public string? FeaturedImage { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
