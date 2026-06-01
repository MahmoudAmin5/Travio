# Quickstart: Implementing Destination Reviews

## Prerequisites
- .NET 9 SDK
- SQL Server configured for `Travio.Infrastructure`
- Existing JWT auth flow working in Travio API

## 1. Add domain and persistence model
1. Create `DestinationReview` entity in `Travio.Core/Domain/Entities/Destinations`.
2. Add `DbSet<DestinationReview>` to `ApplicationDbContext`.
3. Add EF configuration with:
   - FK to Destination and ApplicationUser
   - max comment length 500
   - filtered unique index for active reviews by `(DestinationId, UserId)`
4. Add migration and update database.

## 2. Add DTOs and validation
1. Create review request/response DTOs in `Travio.Core/DTOs/DestinationDTO`.
2. Add validator for review upsert/update (`rating`, `comment`).
3. Ensure whitespace-only comment is normalized to null.

## 3. Add specifications and service logic
1. Add specs for:
   - listing active reviews with cursor pagination
   - finding active review by user+destination
   - computing aggregate stats for destination
2. Extend `IDestinationService` and implement in `DestinationService`:
   - `GetReviewsAsync`
   - `UpsertMyReviewAsync`
   - `UpdateMyReviewAsync`
   - `DeleteMyReviewAsync` (soft delete)
3. After each mutation, recalculate and persist `Destination.Rating` and `Destination.TotalReviews`.

## 4. Add API endpoints
In `DestinationsController`, add:
- `GET /api/destinations/{destinationId}/reviews`
- `POST /api/destinations/{destinationId}/reviews`
- `PUT /api/destinations/{destinationId}/reviews/me`
- `DELETE /api/destinations/{destinationId}/reviews/me`

Ensure auth is required for mutating endpoints and ownership checks are enforced.

## 5. Verification checklist
- [ ] Cannot submit rating outside 1..5
- [ ] Cannot submit comment > 500 chars
- [ ] Second submit by same user updates existing active review
- [ ] Delete marks review inactive (not removed)
- [ ] List excludes inactive reviews
- [ ] Destination aggregates update after add/update/delete
- [ ] Empty destination review list returns explicit empty-state payload
- [ ] Pagination returns stable cursor progression and no duplicate active rows

## 6. Suggested tests
- Service-level tests for business rules and aggregate updates
- API integration tests for auth + ownership + validation paths
- Pagination tests for cursor stability with inserts between page requests
