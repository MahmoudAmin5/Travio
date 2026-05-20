using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.DTOs.GenericResponse;
using Travio.Core.DTOs.TripPlanerDTOs;
using Travio.Core.Helpers;

namespace Travio.API.Controllers;

[Route("api/chat")]
[ApiController]
[Authorize]
public class ChatHistoryController : ControllerBase
{
    private readonly IChatHistoryService _chatHistoryService;

    public ChatHistoryController(IChatHistoryService chatHistoryService)
    {
        _chatHistoryService = chatHistoryService;
    }

    /// <summary>
    /// Get all chat sessions for the authenticated user (paginated, most recent first).
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ServiceResponse<Pagination<ChatSessionSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();
        var result = await _chatHistoryService.GetUserSessionsAsync(pageIndex, pageSize, userId);
        return Ok(result);
    }

    /// <summary>
    /// Get all messages in a chat session (paginated).
    /// </summary>
    [HttpGet("sessions/{id}/messages")]
    [ProducesResponseType(typeof(ServiceResponse<Pagination<ChatMessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<Pagination<ChatMessageDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionMessages(int id, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 50)
    {
        var userId = User.GetUserId();
        var result = await _chatHistoryService.GetSessionMessagesAsync(id, pageIndex, pageSize, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Delete a chat session and all its messages.
    /// </summary>
    [HttpDelete("sessions/{id}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(int id)
    {
        var userId = User.GetUserId();
        var result = await _chatHistoryService.DeleteSessionAsync(id, userId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
