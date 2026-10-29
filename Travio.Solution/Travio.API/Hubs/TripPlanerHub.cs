using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
    private readonly IUnitOfWork _unitOfWork;

    public TripPlanerHub(
        ITripPlanerService tripPlanerService,
        IGenericRepository<ChatSession> sessionRepo,
        IGenericRepository<ChatMessage> messageRepo,
        IGenericRepository<SavedTrip> tripRepo,
        IUnitOfWork unitOfWork)
    {
        _tripPlanerService = tripPlanerService;
        _sessionRepo = sessionRepo;
        _messageRepo = messageRepo;
        _tripRepo = tripRepo;
        _unitOfWork = unitOfWork;
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

        // --- Persist: Get or create ChatSession + Save user message atomically ---
        var chatSessionId = await GetOrCreateSessionAndSaveUserMessageAsync(userId, threadId, userMessage);

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

            // --- Persist: Save assistant message & update session timestamp ---
            var messageType = response.Status == "processing" ? "status" : "text";
            await _unitOfWork.ExecuteAsync(async () =>
            {
                await SaveMessageInternalAsync(chatSessionId, "assistant", textToStream, messageType);

                // Rule 3: Set-based update on ChatSession timestamp
                await _sessionRepo.GetQueryable()
                    .Where(s => s.Id == chatSessionId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, DateTimeOffset.UtcNow));
            });

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

                    if (statusResponse.Status?.ToLower() == "complete" ||
                        statusResponse.Status?.ToLower() == "completed" ||
                        statusResponse.Status?.ToLower() == "success" ||
                        statusResponse.Data != null)
                    {
                        await Clients.Client(connectionId).SendAsync("ReceiveItineraryStatus", statusResponse);

                        // --- Persist: Auto-save the completed trip and itinerary message atomically ---
                        if (statusResponse.Data != null)
                        {
                            var savedTrip = await _unitOfWork.ExecuteAsync(async () =>
                            {
                                var trip = await SaveTripFromItineraryInternalAsync(userId, chatSessionId, statusResponse.Data);
                                var itineraryJson = JsonSerializer.Serialize(statusResponse.Data);
                                await SaveMessageInternalAsync(chatSessionId, "assistant", itineraryJson, "itinerary");
                                return trip;
                            });

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

    private async Task<int> GetOrCreateSessionAndSaveUserMessageAsync(string userId, string threadId, string firstMessage)
    {
        return await _unitOfWork.ExecuteAsync(async () =>
        {
            var now = DateTimeOffset.UtcNow;

            // Rule 3 Upsert Pattern: Try ExecuteUpdateAsync first
            var rowsAffected = await _sessionRepo.GetQueryable()
                .Where(s => s.ThreadId == threadId && s.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, now));

            int sessionId;
            if (rowsAffected == 0)
            {
                var title = firstMessage.Length > 50 ? firstMessage[..50] + "..." : firstMessage;
                var session = new ChatSession
                {
                    UserId = userId,
                    ThreadId = threadId,
                    Title = title,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _sessionRepo.AddAsync(session);
                await _unitOfWork.SaveChangesAsync();
                sessionId = session.Id;
            }
            else
            {
                sessionId = await _sessionRepo.GetQueryable()
                    .AsNoTracking()
                    .Where(s => s.ThreadId == threadId && s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstAsync();
            }

            await SaveMessageInternalAsync(sessionId, "user", firstMessage, "text");
            return sessionId;
        });
    }

    private async Task SaveMessageInternalAsync(int sessionId, string role, string content, string messageType)
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

    private async Task<SavedTrip> SaveTripFromItineraryInternalAsync(string userId, int chatSessionId, ItineraryData data)
    {
        var totalDays = data.Itinerary?.Count ?? 0;
        var firstTheme = data.Itinerary?.FirstOrDefault()?.Theme;
        var title = !string.IsNullOrEmpty(data.CityName)
            ? $"{totalDays}-Day Trip to {data.CityName}"
            : !string.IsNullOrEmpty(firstTheme)
                ? $"{totalDays}-Day Trip: {firstTheme}"
                : $"{totalDays}-Day Trip Plan";

        var rawJson = JsonSerializer.Serialize(data);

        var savedTrip = new SavedTrip
        {
            UserId = userId,
            ChatSessionId = chatSessionId,
            Title = title,
            DestinationName = data.CityName,
            CityHeroImage = data.CityHeroImage,
            TotalDays = totalDays,
            RawJson = rawJson,
            IsFavorite = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (data.Itinerary != null)
        {
            foreach (var dailyPlan in data.Itinerary)
            {
                var day = new SavedTripDay
                {
                    DayNumber = dailyPlan.Day ?? 0,
                    Theme = dailyPlan.Theme
                };

                if (dailyPlan.Activities != null)
                {
                    foreach (var activity in dailyPlan.Activities)
                    {
                        day.Activities.Add(new SavedTripActivity
                        {
                            ActivityType = activity.ActivityType,
                            PlaceName = activity.PlaceName,
                            SuggestedTime = activity.SuggestedTime,
                            Description = activity.Description,
                            Address = activity.Address,
                            FeaturedImage = activity.FeaturedImage,
                            Latitude = activity.Coordinates?.Latitude,
                            Longitude = activity.Coordinates?.Longitude
                        });
                    }
                }

                savedTrip.Days.Add(day);
            }
        }

        if (data.RecommendedHotels != null)
        {
            foreach (var hotel in data.RecommendedHotels)
            {
                savedTrip.Hotels.Add(new SavedTripHotel
                {
                    Name = hotel.Name,
                    Description = hotel.Description,
                    Rating = hotel.Rating,
                    Address = hotel.Address,
                    Link = hotel.Link,
                    FeaturedImage = hotel.FeaturedImage,
                    Latitude = hotel.Coordinates?.Latitude,
                    Longitude = hotel.Coordinates?.Longitude
                });
            }
        }

        await _tripRepo.AddAsync(savedTrip);
        return savedTrip;
    }

    #endregion
}
