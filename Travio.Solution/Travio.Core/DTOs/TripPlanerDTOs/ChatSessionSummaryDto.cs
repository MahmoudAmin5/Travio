namespace Travio.Core.DTOs.TripPlanerDTOs;

public class ChatSessionSummaryDto
{
    public int Id { get; set; }
    public string ThreadId { get; set; } = null!;
    public string? Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool HasTrip { get; set; }
    public int? TripId { get; set; }
}
