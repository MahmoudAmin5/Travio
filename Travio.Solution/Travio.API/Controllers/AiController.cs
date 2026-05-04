using Microsoft.AspNetCore.Mvc;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.DTOs.TripPlanerDTOs;

namespace Travio.API.Controllers;

[ApiController]
[Route("api/[controller]")]

// this controller only for test the Sevice and the connection with the AI,
// and then will use SignalR
public class AiController : ControllerBase
{
    private readonly ITripPlanerService _aiService;

    public AiController(ITripPlanerService aiService)
    {
        _aiService = aiService;
    }

    // POST: api/ai/chat
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequestDto request)
    {
        try
        {
            var result = await _aiService.SendMessageAsync(request);

            if (result == null)
                return BadRequest(new { message = "Failed to get a valid response from the AI service." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while communicating with the AI.", details = ex.Message });
        }
    }

    // GET: api/ai/status/{threadId}
    [HttpGet("status/{threadId}")]
    public async Task<IActionResult> GetStatus(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            return BadRequest(new { message = "Thread ID cannot be null or empty." });
        }

        try
        {
            var result = await _aiService.CheckItineraryStatusAsync(threadId);

            if (result == null)
                return NotFound(new { message = "Itinerary not found or the AI is still processing." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching the itinerary status.", details = ex.Message });
        }
    }
}