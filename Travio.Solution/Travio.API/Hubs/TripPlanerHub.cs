using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.Json;
using Travio.Core.Contracts.Services.TripPlaner;
using Travio.Core.Domain.Entities.TripPlaner;
using Travio.Core.Domain.Infrastructure.Contract;
using Travio.Core.DTOs.TripPlanerDTOs;

namespace Travio.API.Hubs;

[Authorize]
public class TripPlanerHub : Hub
{
    private readonly ITripPlanerService _tripPlanerService;
    private readonly IGenericRepository<ChatSession> _sessionRepo;
    private readonly IGenericRepository<ChatMessage> _messageRepo;
    private readonly IGenericRepository<SavedTrip> _tripRepo;
    private readonly IGenericRepository<SavedTripDay> _dayRepo;
    private readonly IGenericRepository<SavedTripActivity> _activityRepo;
    private readonly IGenericRepository<SavedTripHotel> _hotelRepo;

    public TripPlanerHub(
        ITripPlanerService tripPlanerService,
        IGenericRepository<ChatSession> sessionRepo,
        IGenericRepository<ChatMessage> messageRepo,
        IGenericRepository<SavedTrip> tripRepo,
        IGenericRepository<SavedTripDay> dayRepo,
        IGenericRepository<SavedTripActivity> activityRepo,
        IGenericRepository<SavedTripHotel> hotelRepo)
    {
        _tripPlanerService = tripPlanerService;
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _tripRepo = tripRepo;
        _dayRepo = dayRepo;
        _activityRepo = activityRepo;
        _hotelRepo = hotelRepo;
    }

    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new HubException("User is not authenticated.");

    public async Task SendMessage(string threadId, string userMessage)
    {
        var connectionId = Context.ConnectionId;
        var userId = GetUserId();

        // 1. Notify client that AI is thinking
        await Clients.Client(connectionId).SendAsync("ReceiveStatus", "thinking");

        // --- Persist: Get or create ChatSession ---
        var chatSession = await GetOrCreateSessionAsync(userId, threadId, userMessage);

        // --- Persist: Save user message ---
        await SaveMessageAsync(chatSession.Id, "user", userMessage, "text");

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

            // --- Persist: Save assistant message ---
            var messageType = response.Status == "processing" ? "status" : "text";
            await SaveMessageAsync(chatSession.Id, "assistant", textToStream, messageType);

            // Update session timestamp
            chatSession.UpdatedAt = DateTimeOffset.UtcNow;
            await _sessionRepo.UpdateAsync(chatSession);

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

                        // --- Persist: Auto-save the completed trip ---
                        if (statusResponse.Data != null)
                        {
                            var savedTrip = await SaveTripFromItineraryAsync(userId, chatSession.Id, statusResponse.Data);

                            // Save itinerary as assistant message
                            var itineraryJson = JsonSerializer.Serialize(statusResponse.Data);
                            await SaveMessageAsync(chatSession.Id, "assistant", itineraryJson, "itinerary");

                            // Notify the client that the trip was saved
                            await Clients.Client(connectionId).SendAsync("TripSaved", new
                            {
                                tripId = savedTrip.Id,
                                title = savedTrip.Title
                            });
                        }

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

    #region Private Persistence Helpers

    private async Task<ChatSession> GetOrCreateSessionAsync(string userId, string threadId, string firstMessage)
    {
        // Try to find existing session by threadId
        var allSessions = await _sessionRepo.ListAsync();
        var existing = allSessions.FirstOrDefault(s => s.ThreadId == threadId && s.UserId == userId);

        if (existing != null)
            return existing;

        // Create a new session
        var title = firstMessage.Length > 50 ? firstMessage[..50] + "..." : firstMessage;
        var session = new ChatSession
        {
            UserId = userId,
            ThreadId = threadId,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _sessionRepo.AddAsync(session);
        return session;
    }

    private async Task SaveMessageAsync(int sessionId, string role, string content, string messageType)
    {
        var message = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = role,
            Content = content,
            MessageType = messageType,
            SentAt = DateTimeOffset.UtcNow
        };

        await _messageRepo.AddAsync(message);
    }

    private async Task<SavedTrip> SaveTripFromItineraryAsync(string userId, int chatSessionId, ItineraryData data)
    {
        // Derive a title from the itinerary
        var totalDays = data.Itinerary?.Count ?? 0;
        var firstTheme = data.Itinerary?.FirstOrDefault()?.Theme;
        var title = !string.IsNullOrEmpty(firstTheme)
            ? $"{totalDays}-Day Trip: {firstTheme}"
            : $"{totalDays}-Day Trip Plan";

        var rawJson = JsonSerializer.Serialize(data);

        var savedTrip = new SavedTrip
        {
            UserId = userId,
            ChatSessionId = chatSessionId,
            Title = title,
            TotalDays = totalDays,
            RawJson = rawJson,
            IsFavorite = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _tripRepo.AddAsync(savedTrip);

        // Save days and activities
        if (data.Itinerary != null)
        {
            foreach (var dailyPlan in data.Itinerary)
            {
                var day = new SavedTripDay
                {
                    SavedTripId = savedTrip.Id,
                    DayNumber = dailyPlan.Day ?? 0,
                    Theme = dailyPlan.Theme
                };

                await _dayRepo.AddAsync(day);

                if (dailyPlan.Activities != null)
                {
                    foreach (var activity in dailyPlan.Activities)
                    {
                        var savedActivity = new SavedTripActivity
                        {
                            SavedTripDayId = day.Id,
                            ActivityType = activity.ActivityType,
                            PlaceName = activity.PlaceName,
                            SuggestedTime = activity.SuggestedTime,
                            Description = activity.Description,
                            Address = activity.Address,
                            FeaturedImage = activity.FeaturedImage
                        };

                        await _activityRepo.AddAsync(savedActivity);
                    }
                }
            }
        }

        // Save hotels
        if (data.RecommendedHotels != null)
        {
            foreach (var hotel in data.RecommendedHotels)
            {
                var savedHotel = new SavedTripHotel
                {
                    SavedTripId = savedTrip.Id,
                    Name = hotel.Name,
                    Description = hotel.Description,
                    Rating = hotel.Rating,
                    Address = hotel.Address,
                    Link = hotel.Link,
                    FeaturedImage = hotel.FeaturedImage
                };

                await _hotelRepo.AddAsync(savedHotel);
            }
        }

        return savedTrip;
    }

    #endregion
}
