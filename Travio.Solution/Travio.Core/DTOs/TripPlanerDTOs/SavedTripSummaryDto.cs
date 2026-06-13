namespace Travio.Core.DTOs.TripPlanerDTOs;

public class SavedTripSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? DestinationName { get; set; }
    public string? CityHeroImage { get; set; }
    public int TotalDays { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
