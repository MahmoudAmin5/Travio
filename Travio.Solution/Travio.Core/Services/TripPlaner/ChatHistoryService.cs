using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.Domain.Entities.TripPlaner;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.TripPlanerDTOs;
using Travio.Core.Helpers;

namespace Travio.Core.Services.TripPlaner;

public class ChatHistoryService : IChatHistoryService
{
    private readonly IGenericRepository<ChatSession> _sessionRepo;
    private readonly IGenericRepository<ChatMessage> _messageRepo;
    private readonly IGenericRepository<SavedTrip> _tripRepo;

    public ChatHistoryService(
        IGenericRepository<ChatSession> sessionRepo,
        IGenericRepository<ChatMessage> messageRepo,
        IGenericRepository<SavedTrip> tripRepo)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _tripRepo = tripRepo;
    }

    public async Task<ServiceResponse<Pagination<ChatSessionSummaryDto>>> GetUserSessionsAsync(int pageIndex, int pageSize, string userId)
    {
        var allSessions = await _sessionRepo.ListAsync();
        var userSessions = allSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();

        var allTrips = await _tripRepo.ListAsync();

        var totalCount = userSessions.Count;
        var pagedSessions = userSessions
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(s =>
            {
                var trip = allTrips.FirstOrDefault(t => t.ChatSessionId == s.Id);
                return new ChatSessionSummaryDto
                {
                    Id = s.Id,
                    ThreadId = s.ThreadId,
                    Title = s.Title,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    HasTrip = trip != null,
                    TripId = trip?.Id
                };
            })
            .ToList();

        var pagination = new Pagination<ChatSessionSummaryDto>(pageIndex, pageSize, totalCount, pagedSessions);
        return new ServiceResponse<Pagination<ChatSessionSummaryDto>>(pagination, "Sessions retrieved successfully.");
    }

    public async Task<ServiceResponse<Pagination<ChatMessageDto>>> GetSessionMessagesAsync(int sessionId, int pageIndex, int pageSize, string userId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            return new ServiceResponse<Pagination<ChatMessageDto>>("Session not found.");

        var allMessages = await _messageRepo.ListAsync();
        var sessionMessages = allMessages
            .Where(m => m.ChatSessionId == sessionId)
            .OrderBy(m => m.SentAt)
            .ToList();

        var totalCount = sessionMessages.Count;
        var pagedMessages = sessionMessages
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                MessageType = m.MessageType,
                SentAt = m.SentAt
            })
            .ToList();

        var pagination = new Pagination<ChatMessageDto>(pageIndex, pageSize, totalCount, pagedMessages);
        return new ServiceResponse<Pagination<ChatMessageDto>>(pagination, "Messages retrieved successfully.");
    }

    public async Task<ServiceResponse<bool>> DeleteSessionAsync(int sessionId, string userId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.UserId != userId)
            return new ServiceResponse<bool>("Session not found.");

        await _sessionRepo.DeleteAsync(session);
        return new ServiceResponse<bool>(true, "Session deleted successfully.");
    }
}
