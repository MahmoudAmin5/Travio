using System.Net.Http.Json;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.DTOs.TripPlanerDTOs;

namespace Travio.Core.Services.TripPlaner;

public class TripPlanerService : ITripPlanerService
{
    private readonly HttpClient _httpClient;
    public TripPlanerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    public async Task<AiChatResponseDto> SendMessageAsync(AiChatRequestDto request)
    {

        var response = await _httpClient.PostAsJsonAsync("chat", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AiChatResponseDto>();
            return result ?? throw new Exception("Failed to deserialize the AI response. Response was null.");
        }

        throw new Exception($"Failed to connect to AI: {response.StatusCode}");
    }
    public async Task<AiStatusResponseDto> CheckItineraryStatusAsync(string threadId)
    {
        var response = await _httpClient.GetAsync($"status/{threadId}");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AiStatusResponseDto>();
            return result ?? throw new Exception("Failed to deserialize the AI response. Response was null.");
        }

        throw new Exception($"Failed to get status: {response.StatusCode}");
    }

}
