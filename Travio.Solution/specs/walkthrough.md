# Walkthrough: AI Trip & Chat Persistence Feature

> **Branch:** `feature/trip-chat-persistence`  
> **Base:** `master` (commit `0daacff`)  
> **Commit:** `bc7f119`  
> **Date:** 2026-05-21  
> **Build:** ✅ 0 errors, 263 warnings (all pre-existing)

---

## Summary

This feature adds **3 major capabilities** to the Travio backend:

1. **Trip Persistence** — AI-generated itineraries are automatically saved to the database when the AI completes them
2. **Chat History** — Every message sent/received through the SignalR hub is saved, allowing users to revisit past conversations
3. **Favorite Trips** — Users can mark/unmark trips as favorites and retrieve only their favorites

**22 files changed** — 19 new files + 3 modified files.

---

## Files to Review

### Layer 1: Domain Entities (Travio.Core)

These are the new database models — review for correct relationships, data types, and naming.

| File | Description |
|---|---|
| [ChatSession.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/TripPlaner/ChatSession.cs) | Stores per-user AI chat sessions with `ThreadId` link to external AI |
| [ChatMessage.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/TripPlaner/ChatMessage.cs) | Individual messages within a session (`user`/`assistant` roles) |
| [SavedTrip.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/TripPlaner/SavedTrip.cs) | Saved itinerary with `IsFavorite` flag + raw JSON backup |
| [SavedTripDay.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/TripPlaner/SavedTripDay.cs) | A single day in the itinerary |
| [SavedTripActivity.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/TripPlaner/SavedTripActivity.cs) | An activity within a day |
| [SavedTripHotel.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/TripPlaner/SavedTripHotel.cs) | Recommended hotel for a trip |
| [ApplicationUser.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Domain/Entities/Account_Mangement/ApplicationUser.cs) | **Modified** — added `ChatSessions` and `SavedTrips` navigation properties |

---

### Layer 2: DTOs (Travio.Core)

Review for correct property exposure to mobile clients.

| File | Description |
|---|---|
| [SavedTripSummaryDto.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/DTOs/TripPlanerDTOs/SavedTripSummaryDto.cs) | Lightweight DTO for trip list views |
| [SavedTripDetailDto.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/DTOs/TripPlanerDTOs/SavedTripDetailDto.cs) | Full trip detail with nested days/activities/hotels |
| [ChatSessionSummaryDto.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/DTOs/TripPlanerDTOs/ChatSessionSummaryDto.cs) | Chat session list item with `HasTrip`/`TripId` indicators |
| [ChatMessageDto.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/DTOs/TripPlanerDTOs/ChatMessageDto.cs) | Individual chat message DTO |

---

### Layer 3: Service Contracts & Implementations (Travio.Core)

Review for business logic correctness and ownership validation.

| File | Description |
|---|---|
| [ISavedTripService.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Contracts/Services/TripPlaner/ISavedTripService.cs) | Interface: list trips, get detail, toggle favorite, delete |
| [IChatHistoryService.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Contracts/Services/TripPlaner/IChatHistoryService.cs) | Interface: list sessions, get messages, delete session |
| [SavedTripService.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Services/TripPlaner/SavedTripService.cs) | Implementation with pagination + user ownership checks |
| [ChatHistoryService.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Core/Services/TripPlaner/ChatHistoryService.cs) | Implementation with pagination + user ownership checks |

---

### Layer 4: EF Core Configurations (Travio.Infrastructure)

Review for proper constraints, indexes, and cascade behavior.

| File | Description |
|---|---|
| [ChatSessionConfiguration.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Infrastructure/Configrations/TripPlaner/ChatSessionConfiguration.cs) | Config for `ChatSession` + `ChatMessage` (indexes on UserId, ThreadId) |
| [SavedTripConfiguration.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Infrastructure/Configrations/TripPlaner/SavedTripConfiguration.cs) | Config for `SavedTrip`, `SavedTripDay`, `SavedTripActivity`, `SavedTripHotel` |
| [ApplicationDbContext.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.Infrastructure/ApplicationDbContext.cs) | **Modified** — added 6 new `DbSet<>` properties |

---

### Layer 5: API Controllers & Hub (Travio.API)

Review for correct endpoint design, auth, and error handling.

| File | Description |
|---|---|
| [TripController.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.API/Controllers/TripController.cs) | REST endpoints: `GET /api/trip`, `GET /api/trip/favorites`, `GET /api/trip/{id}`, `POST /api/trip/{id}/favorite`, `DELETE /api/trip/{id}` |
| [ChatHistoryController.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.API/Controllers/ChatHistoryController.cs) | REST endpoints: `GET /api/chat/sessions`, `GET /api/chat/sessions/{id}/messages`, `DELETE /api/chat/sessions/{id}` |
| [TripPlanerHub.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.API/Hubs/TripPlanerHub.cs) | **Major rewrite** — added `[Authorize]`, auto-saves messages and trips, fires `TripSaved` event |
| [ServiceCollectionExtensions.cs](file:///d:/work/GraduationProject/Travio/Travio/Travio.Solution/Travio.API/Extensions/ServiceCollectionExtensions.cs) | **Modified** — registered new services + added SignalR JWT query string token extraction |

---

## Key Design Decisions

### 1. Trip Favorite as Boolean (not Junction Table)
Since a `SavedTrip` already belongs to exactly one user, the `IsFavorite` boolean flag is simpler and more efficient than a separate `UserFavoriteTrip` junction table. This is different from the existing `UserFavorite` (for destinations) which uses a junction table because destinations are shared across users.

### 2. Raw JSON Backup
Each `SavedTrip` stores the raw AI JSON response in `RawJson`. This provides a fallback if the structured data model changes, and allows the mobile app to render custom layouts if needed.

### 3. SignalR Authentication
Added JWT token extraction from `?access_token=` query parameter in `JwtBearerEvents.OnMessageReceived`. This is the standard approach for SignalR WebSocket connections since WebSockets can't send custom HTTP headers.

### 4. Auto-Save (No Manual Save Endpoint)
Trips and messages are automatically saved during the SignalR conversation flow — no separate "save" button needed on mobile. This ensures no data loss even if the user closes the app.

---

## Verification

- ✅ **Build**: `dotnet build` — 0 errors, 263 warnings (all pre-existing)

---

## Remaining Steps

> [!IMPORTANT]
> After merging this branch, run the EF migration command to create the new tables:
> ```bash
> dotnet ef migrations add AddChatAndTripEntities --project Travio.Infrastructure --startup-project Travio.API
> dotnet ef database update --project Travio.Infrastructure --startup-project Travio.API
> ```

---

## API Summary for Mobile Developer

### Trip Endpoints (`/api/trip`) — All require `[Authorize]`

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/trip` | List all user's saved trips (paginated) |
| `GET` | `/api/trip/favorites` | List only favorite trips (paginated) |
| `GET` | `/api/trip/{id}` | Get full trip detail (days + activities + hotels) |
| `POST` | `/api/trip/{id}/favorite` | Toggle favorite on/off |
| `DELETE` | `/api/trip/{id}` | Delete a saved trip |

### Chat Endpoints (`/api/chat`) — All require `[Authorize]`

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/chat/sessions` | List all chat sessions (paginated, newest first) |
| `GET` | `/api/chat/sessions/{id}/messages` | Get messages in a session (paginated) |
| `DELETE` | `/api/chat/sessions/{id}` | Delete a session and its messages |

### New SignalR Event

| Event | Payload | When |
|---|---|---|
| `TripSaved` | `{ tripId, title }` | When an AI itinerary is auto-saved to DB |

### SignalR Connection (Updated)
Mobile app must now send JWT token:
```
/hubs/trip-planer?access_token=<JWT_TOKEN>
```
