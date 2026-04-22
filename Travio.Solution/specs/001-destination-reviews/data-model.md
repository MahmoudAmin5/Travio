# Phase 1 Data Model: Destination Reviews

## Entity: DestinationReview

Represents one traveler’s review of one destination.

### Fields
- `ReviewId` (int, PK)
- `DestinationId` (int, FK -> Destination.DestinationID, required)
- `UserId` (string, FK -> ApplicationUser.Id, required)
- `Rating` (int, required, range 1..5)
- `Comment` (string?, optional, max length 500, whitespace-only normalized to null)
- `IsActive` (bool, required, default true)
- `CreatedAtUtc` (DateTime, required)
- `UpdatedAtUtc` (DateTime, required)

### Relationships
- Many `DestinationReview` to one `Destination`
- Many `DestinationReview` to one `ApplicationUser`

### Constraints
- Unique active review per (`DestinationId`, `UserId`) where `IsActive = true`
- Soft-delete behavior: delete transitions `IsActive: true -> false`
- Active reviews only participate in listing and aggregate computations

### State Transitions
- **Create**: no active review exists -> new active row
- **Upsert update**: active review exists -> update `Rating`, `Comment`, `UpdatedAtUtc`
- **Delete**: active review exists -> set `IsActive=false`, update `UpdatedAtUtc`

---

## Existing Aggregate Entity: Destination

Review feature reuses and updates existing aggregate fields.

### Aggregate Fields (existing)
- `Rating` (double) -> average of active review ratings, rounded for presentation to 1 decimal
- `TotalReviews` (int) -> count of active reviews

### Aggregate Update Rule
After each review add/update/delete:
1. Query active reviews for the destination
2. Compute count and average
3. Persist `TotalReviews` and `Rating`
4. If count = 0, set `TotalReviews = 0`, `Rating = 0` (or API-level nullable view model if desired)

---

## Read Model: DestinationReviewPage

Payload returned to mobile app for infinite scrolling.

### Fields
- `DestinationId` (int)
- `AverageRating` (decimal with 1 decimal for response)
- `TotalReviews` (int)
- `Items` (list of `DestinationReviewItem`)
- `NextCursor` (string? null when exhausted)
- `HasMore` (bool)
- `IsEmpty` (bool)

### DestinationReviewItem
- `ReviewId` (int)
- `ReviewerName` (string)
- `ReviewDateUtc` (DateTime)
- `Rating` (int)
- `Comment` (string?)
- `IsMine` (bool, optional convenience field)
