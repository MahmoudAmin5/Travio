using Microsoft.AspNetCore.SignalR;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.DTOs.TripPlanerDTOs;

namespace Travio.API.Hubs;

public class TripPlanerHub : Hub
{
    private readonly ITripPlanerService _tripPlanerService;

    public TripPlanerHub(ITripPlanerService tripPlanerService)
    {
        _tripPlanerService = tripPlanerService;
    }

    public async Task SendMessage(string threadId, string userMessage)
    {
        var connectionId = Context.ConnectionId;

        // 1. Notify client that AI is thinking
        await Clients.Client(connectionId).SendAsync("ReceiveStatus", "thinking");

        var request = new AiChatRequestDto
        {
            ThreadId = threadId,
            Message = userMessage
        };

        try
        {
            // 2. Call the AI service
            var response = await _tripPlanerService.SendMessageAsync(request);

            // Determine which field holds the response text (using Message or Data)
            string textToStream = response.Message ?? response.Data ?? "";

            // 3. Simulate streaming (Typewriter effect)
            int chunkSize = 4; // characters per chunk
            for (int i = 0; i < textToStream.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, textToStream.Length - i);
                string chunk = textToStream.Substring(i, length);

                // Send chunk to the client
                await Clients.Client(connectionId).SendAsync("ReceiveMessageChunk", chunk);

                // Small delay to simulate typing
                await Task.Delay(30);
            }

            // 4. Send completion signal and the full response metadata
            await Clients.Client(connectionId).SendAsync("MessageComplete", response);

            // 5. If the AI indicated that it's processing a plan, start checking the status
            if (response.Status == "processing")
            {
                // Notify client that generation has started
                await Clients.Client(connectionId).SendAsync("ReceiveSystemMessage", "Your plan is generating. Please wait...");

                bool isCompleted = false;
                int maxRetries = 120; // Max wait time: 60 * 10s = 5 minutes
                int delaySeconds = 10;

                for (int i = 0; i < maxRetries; i++)
                {
                    if (Context.ConnectionAborted.IsCancellationRequested)
                        break;

                    await Task.Delay(delaySeconds * 1000, Context.ConnectionAborted);

                    var statusResponse = await _tripPlanerService.CheckItineraryStatusAsync(threadId);

                    // Debugging: Send the raw status back to the client so we can see what it is
                    await Clients.Client(connectionId).SendAsync("ReceiveSystemMessage", $"[Debug] Checked status: '{statusResponse.Status}'");

                    // Check if completed. We also check if Data is populated, which is a strong indicator it's done!
                    if (statusResponse.Status?.ToLower() == "completed" ||
                        statusResponse.Status?.ToLower() == "success" ||
                        statusResponse.Data != null)
                    {
                        await Clients.Client(connectionId).SendAsync("ReceiveItineraryStatus", statusResponse);
                        isCompleted = true;
                        break;
                    }
                    else if (statusResponse.Status?.ToLower() == "failed" || statusResponse.Status?.ToLower() == "error")
                    {
                        await Clients.Client(connectionId).SendAsync("ReceiveError", statusResponse.Message ?? "Failed to generate plan.");
                        isCompleted = true;
                        break;
                    }
                    // If still processing ("processing", "pending", etc.), loop again
                }

                if (!isCompleted && !Context.ConnectionAborted.IsCancellationRequested)
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveError", "Timeout while generating the plan.");
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Client disconnected while we were delaying/waiting, do nothing
        }
        catch (Exception ex)
        {
            // Handle any errors
            await Clients.Client(connectionId).SendAsync("ReceiveError", ex.Message);
        }
    }
}
