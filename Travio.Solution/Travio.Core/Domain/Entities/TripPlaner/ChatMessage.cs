namespace Travio.Core.Domain.Entities.TripPlaner;

public class ChatMessage
{
    public int Id { get; set; }
    public int ChatSessionId { get; set; }
    public string Role { get; set; } = null!; // "user" or "assistant"
    public string Content { get; set; } = null!;
    public string MessageType { get; set; } = "text"; // "text", "itinerary", "status"
    public DateTimeOffset SentAt { get; set; }

    // Navigation
    public ChatSession ChatSession { get; set; } = null!;
}
