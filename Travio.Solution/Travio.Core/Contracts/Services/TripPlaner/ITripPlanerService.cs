using Travio.Core.DTOs.TripPlanerDTOs;

namespace Travio.Core.Contracts.Services.TripPlaner;

public interface ITripPlanerService
{
    Task<AiChatResponseDto> SendMessageAsync(AiChatRequestDto request);
    Task<AiStatusResponseDto> CheckItineraryStatusAsync(string threadId);
}
