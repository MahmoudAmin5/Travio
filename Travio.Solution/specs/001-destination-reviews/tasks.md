# Tasks: Destination Reviews

**Input**: Design documents from `/specs/001-destination-reviews/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: No explicit TDD/testing mandate in spec.md; test tasks are excluded for this task list.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create shared feature scaffolding and request/response contracts used by all stories.

- [X] T001 Create review DTO contracts in Travio.Core/DTOs/DestinationDTO/DestinationReviewDto.cs
- [X] T002 [P] Create review write DTO in Travio.Core/DTOs/DestinationDTO/DestinationReviewUpsertDto.cs
- [X] T003 [P] Create cursor page DTO in Travio.Core/DTOs/DestinationDTO/DestinationReviewPageDto.cs
- [X] T004 Add review input validator in Travio.Core/Validators/DestinationReviewUpsertValidator.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement core data model, persistence, service contracts, and common specs required by all stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 Create DestinationReview entity in Travio.Core/Domain/Entities/Destinations/DestinationReview.cs
- [X] T006 Add DestinationReview DbSet in Travio.Infrastructure/ApplicationDbContext.cs
- [X] T007 Configure DestinationReview EF mapping and filtered unique index in Travio.Infrastructure/Configrations/Destinations/DestinationReviewConfiguration.cs
- [X] T008 Create migration for DestinationReview schema in Travio.Infrastructure/Migrations/<timestamp>_add_destination_reviews.cs
- [X] T009 Extend destination service contract with review methods in Travio.Core/Contracts/Services/Destination/IDestinationService.cs
- [X] T010 [P] Add specification to fetch active review by user and destination in Travio.Core/Domain/Specifications/Destinations/DestinationReviewByUserAndDestinationSpec.cs
- [X] T011 [P] Add specification to compute active destination review aggregates in Travio.Core/Domain/Specifications/Destinations/ActiveDestinationReviewStatsSpec.cs

**Checkpoint**: Foundation ready - user story implementation can now begin.

---

## Phase 3: User Story 1 - View destination reviews (Priority: P1) 🎯 MVP

**Goal**: Travelers can read average rating and reverse-chronological reviews with cursor pagination and explicit empty state.

**Independent Test**: Call `GET /api/destinations/{destinationId}/reviews` for destinations with and without active reviews and verify payload fields, newest-first order, and cursor behavior.

### Implementation for User Story 1

- [X] T012 [P] [US1] Add specification for cursor-based active review listing in Travio.Core/Domain/Specifications/Destinations/DestinationReviewsByDestinationSpec.cs
- [X] T013 [P] [US1] Add Mapster mapping for review read models in Travio.Core/MappingProfiles/MappingProfile.cs
- [X] T014 [US1] Implement GetReviewsAsync with empty-state payload and 1-decimal aggregate formatting in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T015 [US1] Add GET destination reviews endpoint in Travio.API/Controllers/DestinationsController.cs
- [X] T016 [US1] Add cursor parsing/encoding helper for review pagination in Travio.Core/Helpers/DestinationReviewCursor.cs
- [X] T017 [US1] Wire cursor helper usage in review retrieval service flow in Travio.Core/Services/Destinations/DestinationService.cs

**Checkpoint**: User Story 1 is independently functional and delivers MVP browse value.

---

## Phase 4: User Story 2 - Add or update a personal review (Priority: P2)

**Goal**: Authenticated travelers can create or upsert one active review per destination with validation and aggregate recalculation.

**Independent Test**: Authenticate, call `POST /api/destinations/{destinationId}/reviews` twice for same destination, and verify second call updates existing active review (not duplicate) and refreshes aggregates.

### Implementation for User Story 2

- [X] T018 [P] [US2] Add destination service helper to normalize/validate review comment text in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T019 [US2] Implement UpsertMyReviewAsync with one-active-review enforcement in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T020 [US2] Implement aggregate refresh routine after add/update in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T021 [US2] Add POST upsert review endpoint with [Authorize] in Travio.API/Controllers/DestinationsController.cs
- [X] T022 [US2] Return validation and not-found responses for review upsert in Travio.API/Controllers/DestinationsController.cs
- [X] T023 [US2] Ensure destination service registration still resolves updated contract in Travio.API/Extensions/ServiceCollectionExtensions.cs

**Checkpoint**: User Stories 1 and 2 both work independently.

---

## Phase 5: User Story 3 - Edit or delete my review (Priority: P3)

**Goal**: Review owners can explicitly edit or soft-delete their review, with ownership checks and aggregate updates.

**Independent Test**: Authenticate as review owner and call `PUT` then `DELETE /api/destinations/{destinationId}/reviews/me`; verify soft delete (`IsActive=false`), ownership enforcement, and aggregate recalculation.

### Implementation for User Story 3

- [X] T024 [US3] Implement UpdateMyReviewAsync with last-write-wins semantics in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T025 [US3] Implement DeleteMyReviewAsync using soft delete in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T026 [US3] Reuse aggregate refresh routine after delete in Travio.Core/Services/Destinations/DestinationService.cs
- [X] T027 [US3] Add PUT and DELETE my-review endpoints with ownership handling in Travio.API/Controllers/DestinationsController.cs
- [X] T028 [US3] Add ownership/authorization error mapping for edit/delete paths in Travio.API/Controllers/DestinationsController.cs

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final alignment, documentation, and end-to-end validation across all stories.

- [X] T029 [P] Update API contract examples and error payloads in specs/001-destination-reviews/contracts/destination-reviews-api.md
- [X] T030 [P] Update implementation quickstart with final endpoint and payload details in specs/001-destination-reviews/quickstart.md
- [ ] T031 Validate migration + API startup + feature flow in specs/001-destination-reviews/quickstart.md
- [X] T032 Run full solution build after all changes in Travio.Solution.sln

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1 and blocks all user stories.
- **Phase 3 (US1)**: Depends on Phase 2.
- **Phase 4 (US2)**: Depends on Phase 2 and integrates with shared review infrastructure.
- **Phase 5 (US3)**: Depends on Phase 2 and review mutation logic from US2.
- **Phase 6 (Polish)**: Depends on completion of desired user stories.

### User Story Dependency Graph

- **US1 (P1)** → independent after Foundational.
- **US2 (P2)** → independent after Foundational, but reuses shared contracts/specs.
- **US3 (P3)** → depends on review mutation paths introduced in US2.

Graph: `US1` and `US2` can start after Phase 2; `US3` follows `US2`.

### Within Each User Story

- Specifications/helpers before service implementation.
- Service implementation before controller endpoints.
- Endpoint behavior + error mapping before polish validation.

---

## Parallel Execution Examples

### User Story 1

- Run T012 and T013 in parallel (different files).
- After T014 starts, T016 can be implemented in parallel, then merged via T017.

### User Story 2

- T018 can run in parallel with API-layer preparation in T022.
- T021 and T022 can proceed after T019/T020 are stable.

### User Story 3

- T024 and T025 can be split between two developers in the same service file only if coordinated sequentially by method ownership.
- T027 and T028 can proceed together once service methods are complete.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 (US1).
3. Validate review browsing (average, list ordering, cursor pagination, empty state).
4. Demo/deploy MVP read-only value.

### Incremental Delivery

1. Deliver MVP browse flow (US1).
2. Add authenticated upsert and validation rules (US2).
3. Add owner edit/delete with soft-delete semantics (US3).
4. Finish with cross-cutting documentation and build verification (Phase 6).

### Parallel Team Strategy

1. One developer handles data/persistence (T005-T008) while another prepares contracts/specs (T009-T011) where possible.
2. After Foundation:
   - Dev A: US1 endpoints + pagination.
   - Dev B: US2 mutation logic.
3. Dev C adds US3 owner management once US2 service paths are stable.

---

## Notes

- [P] tasks are parallelizable where file-level conflicts are minimal.
- [US#] labels map tasks to specific user stories for traceability.
- Every task includes an explicit file path.
- Build verification (T032) is the final quality gate before completion.
