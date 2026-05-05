using System.Text.Json.Serialization;

namespace Travio.Core.DTOs.TripPlanerDTOs;

public class AiChatResponseDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; } // "chatting" or "processing"

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}