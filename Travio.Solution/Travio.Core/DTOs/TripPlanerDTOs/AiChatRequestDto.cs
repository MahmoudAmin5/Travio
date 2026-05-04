using System.Text.Json.Serialization;

namespace Travio.Core.DTOs.TripPlanerDTOs;

public class AiChatRequestDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("thread_id")]
    public string? ThreadId { get; set; }
}