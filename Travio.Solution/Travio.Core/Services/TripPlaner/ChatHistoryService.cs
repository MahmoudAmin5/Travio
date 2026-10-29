using Microsoft.EntityFrameworkCore;
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

    public ChatHistoryService(
        IGenericRepository<ChatSession> sessionRepo,
        IGenericRepository<ChatMessage> messageRepo)
    {
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
    }

    public async Task<ServiceResponse<Pagination<ChatSessionSummaryDto>>> GetUserSessionsAsync(int pageIndex, int pageSize, string userId)
    {
        var baseQuery = _sessionRepo.GetQueryable()
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        var totalCount = await baseQuery.CountAsync();

        // Rule 6: Direct projection to DTO via .Select(...)
        var pagedSessions = await baseQuery
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ChatSessionSummaryDto
            {
                Id = s.Id,
                ThreadId = s.ThreadId,
                Title = s.Title,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                HasTrip = s.SavedTrip != null,
                TripId = s.SavedTrip != null ? (int?)s.SavedTrip.Id : null
            })
            .ToListAsync();

        var pagination = new Pagination<ChatSessionSummaryDto>(pageIndex, pageSize, totalCount, pagedSessions);
        return new ServiceResponse<Pagination<ChatSessionSummaryDto>>(pagination, "Sessions retrieved successfully.");
    }

    public async Task<ServiceResponse<Pagination<ChatMessageDto>>> GetSessionMessagesAsync(int sessionId, int pageIndex, int pageSize, string userId)
    {
        var sessionExists = await _sessionRepo.GetQueryable()
            .AsNoTracking()
            .AnyAsync(s => s.Id == sessionId && s.UserId == userId);

        if (!sessionExists)
            return new ServiceResponse<Pagination<ChatMessageDto>>("Session not found.");

        var baseQuery = _messageRepo.GetQueryable()
            .AsNoTracking()
            .Where(m => m.ChatSessionId == sessionId);

        var totalCount = await baseQuery.CountAsync();

        // Rule 6: Direct projection to DTO via .Select(...)
        var pagedMessages = await baseQuery
            .OrderBy(m => m.SentAt)
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
            .ToListAsync();

        var pagination = new Pagination<ChatMessageDto>(pageIndex, pageSize, totalCount, pagedMessages);
        return new ServiceResponse<Pagination<ChatMessageDto>>(pagination, "Messages retrieved successfully.");
    }

    public async Task<ServiceResponse<bool>> DeleteSessionAsync(int sessionId, string userId)
    {
        // Rule 3: Bulk delete directly via ExecuteDeleteAsync
        var rowsDeleted = await _sessionRepo.GetQueryable()
            .Where(s => s.Id == sessionId && s.UserId == userId)
            .ExecuteDeleteAsync();

        if (rowsDeleted == 0)
            return new ServiceResponse<bool>("Session not found.");

        return new ServiceResponse<bool>(true, "Session deleted successfully.");
    }
}
