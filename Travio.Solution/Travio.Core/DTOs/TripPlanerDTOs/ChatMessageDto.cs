namespace Travio.Core.DTOs.TripPlanerDTOs;

public class ChatMessageDto
{
    public int Id { get; set; }
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string MessageType { get; set; } = "text";
    public DateTimeOffset SentAt { get; set; }
}
