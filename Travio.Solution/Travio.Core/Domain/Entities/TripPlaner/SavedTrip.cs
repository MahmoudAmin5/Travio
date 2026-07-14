using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Core.Domain.Entities.TripPlaner;

public class SavedTrip
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int? ChatSessionId { get; set; }
    public string Title { get; set; } = null!;
    public string? DestinationName { get; set; }
    public string? CityHeroImage { get; set; }
    public int TotalDays { get; set; }
    public string? RawJson { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ChatSession? ChatSession { get; set; }
    public ICollection<SavedTripDay> Days { get; set; } = new HashSet<SavedTripDay>();
    public ICollection<SavedTripHotel> Hotels { get; set; } = new HashSet<SavedTripHotel>();
}
