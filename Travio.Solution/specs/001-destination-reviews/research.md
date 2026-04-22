# Phase 0 Research: Destination Reviews

## Decision 1: Review mutation model should be explicit upsert + owner edit/delete endpoints
- **Decision**: Use an upsert write path for create/update (`POST /api/destinations/{destinationId}/reviews`) and explicit owner management endpoints for edit/delete semantics (`PUT`/`DELETE` for current user review).
- **Rationale**: Matches functional rules (single active review per user/destination), keeps mobile client simple, and preserves clear ownership behavior.
- **Alternatives considered**:
  - Separate create-only and update-only APIs with strict preconditions.
  - Full replacement PATCH model with sparse field updates.

## Decision 2: Enforce uniqueness with both data-level and application-level checks
- **Decision**: Enforce "one active review per user per destination" via application lookup + database unique filtered index on active rows.
- **Rationale**: Prevents duplicates under concurrency and aligns with last-write-wins behavior.
- **Alternatives considered**:
  - Application-level check only (race-condition risk).
  - Hard uniqueness across all rows (incompatible with soft delete and future re-review).

## Decision 3: Aggregate rating recalculation should be computed from active reviews after each mutation
- **Decision**: Recompute `Destination.Rating` and `Destination.TotalReviews` from active reviews after add/update/delete.
- **Rationale**: Correctness-first approach; avoids drift and aligns with soft delete semantics.
- **Alternatives considered**:
  - Increment/decrement running totals (faster but error-prone under retries/concurrency).
  - Deferred background recalculation (eventual consistency not required by spec).

## Decision 4: Cursor-based pagination contract for mobile list performance
- **Decision**: Return reviews using cursor-based pagination ordered by `CreatedAt DESC`, tie-breaker `ReviewId DESC`.
- **Rationale**: Stable pagination when new rows are inserted and fits infinite-scroll UX.
- **Alternatives considered**:
  - Offset/page-number pagination (simple but can skip/duplicate entries under concurrent inserts).
  - Dual pagination modes (extra API complexity).

## Decision 5: Empty-state payload should be explicit and UI-friendly
- **Decision**: Return a structured page payload with `items=[]`, `hasMore=false`, `nextCursor=null`, `isEmpty=true`, and aggregate fields.
- **Rationale**: Allows client to reliably render "Be the first to review!" without special-case parsing.
- **Alternatives considered**:
  - Return HTTP 204 No Content.
  - Return generic list only without explicit empty indicator.

## Decision 6: Validation strategy and domain constraints
- **Decision**: Validate rating in range [1..5], comment optional with max 500 and trim whitespace-only to null.
- **Rationale**: Directly satisfies accepted criteria and avoids storing meaningless comment text.
- **Alternatives considered**:
  - Data annotations only.
  - Accept empty string comments as-is.

## Decision 7: Concurrency handling policy
- **Decision**: Apply last-write-wins based on server processing order for review updates.
- **Rationale**: Matches clarified requirement; minimal client burden.
- **Alternatives considered**:
  - Optimistic concurrency tokens with conflict rejection.
  - Field-level merge.

## Decision 8: Testing strategy (resolved NEEDS CLARIFICATION)
- **Decision**: Add automated tests using xUnit for service-level behavior and API integration flows (authentication/ownership, pagination, aggregate updates).
- **Rationale**: No current test project exists; feature risk centers on business rules and aggregates requiring repeatable validation.
- **Alternatives considered**:
  - Manual verification only.
  - Unit tests without integration coverage for data constraints.
