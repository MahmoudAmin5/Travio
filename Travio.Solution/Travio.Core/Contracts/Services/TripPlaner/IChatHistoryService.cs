using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.TripPlanerDTOs;
using Travio.Core.Helpers;

namespace Travio.Core.Contracts.Services.TripPlaner;

public interface IChatHistoryService
{
    Task<ServiceResponse<Pagination<ChatSessionSummaryDto>>> GetUserSessionsAsync(int pageIndex, int pageSize, string userId);
    Task<ServiceResponse<Pagination<ChatMessageDto>>> GetSessionMessagesAsync(int sessionId, int pageIndex, int pageSize, string userId);
    Task<ServiceResponse<bool>> DeleteSessionAsync(int sessionId, string userId);
}
