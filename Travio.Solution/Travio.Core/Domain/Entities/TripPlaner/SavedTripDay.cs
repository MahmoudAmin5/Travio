namespace Travio.Core.Domain.Entities.TripPlaner;

public class SavedTripDay
{
    public int Id { get; set; }
    public int SavedTripId { get; set; }
    public int DayNumber { get; set; }
    public string? Theme { get; set; }

    // Navigation
    public SavedTrip SavedTrip { get; set; } = null!;
    public ICollection<SavedTripActivity> Activities { get; set; } = new HashSet<SavedTripActivity>();
}
