using Travio.Core.Domain.Entities.Account_Mangement;

namespace Travio.Core.Domain.Entities.TripPlaner;

public class ChatSession
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string ThreadId { get; set; } = null!;
    public string? Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new HashSet<ChatMessage>();
    public SavedTrip? SavedTrip { get; set; }
}
