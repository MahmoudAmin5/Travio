# Implementation Plan: Destination Reviews

**Branch**: `001-destination-reviews` | **Date**: 2026-04-19 | **Spec**: `D:\work\GraduationProject\Travio\Travio\Travio.Solution\specs\001-destination-reviews\spec.md`
**Input**: Feature specification from `/specs/001-destination-reviews/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Add destination review capabilities to the existing ASP.NET Core API and layered architecture: travelers can list reviews with cursor pagination, authenticated users can create/update their single active review per destination, owners can soft-delete their review, and destination rating aggregates (average + total active reviews) are recalculated automatically after any review mutation.

## Technical Context

**Language/Version**: C# / .NET 9 (net9.0)  
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 9 (SQL Server), Ardalis.Specification, Mapster, ASP.NET Core Identity  
**Storage**: SQL Server via EF Core (`ApplicationDbContext`)  
**Testing**: xUnit (new test project to be added for service + integration tests)  
**Target Platform**: ASP.NET Core backend for Travio mobile clients  
**Project Type**: Multi-project backend service (`Travio.API`, `Travio.Core`, `Travio.Infrastructure`)  
**Performance Goals**: Maintain smooth mobile UX for review browsing with incremental loading; retrieval should remain responsive for destinations with high review counts  
**Constraints**: One active review per user per destination; comment max 500 chars; rating 1–5; soft delete required; average displayed at 1 decimal; last-write-wins for concurrent review updates  
**Scale/Scope**: Hundreds of reviews per destination; add one new review aggregate flow plus list/create/update/delete endpoints and data contracts

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution file at `.specify/memory/constitution.md` is a template with placeholders and defines no enforceable project-specific gates.

- Gate status (pre-design): **PASS (No active constitutional gates defined)**
- Gate status (post-design): **PASS (Design artifacts align; no constitutional rules defined to violate)**
- Risk note: Governance principles should be formalized later to enable strict gate enforcement.

## Project Structure

### Documentation (this feature)

```text
specs/001-destination-reviews/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── destination-reviews-api.md
└── tasks.md
```

### Source Code (repository root)

```text
Travio.API/
└── Controllers/
    └── DestinationsController.cs            # Add destination review endpoints

Travio.Core/
├── Contracts/
│   └── Services/
│       └── Destination/
│           └── IDestinationService.cs       # Add review operations
├── Domain/
│   ├── Entities/
│   │   └── Destinations/
│   │       ├── Destination.cs               # Existing aggregate fields reused
│   │       └── DestinationReview.cs         # New entity
│   └── Specifications/
│       └── Destinations/
│           ├── DestinationReviewsByDestinationSpec.cs
│           ├── DestinationReviewByUserAndDestinationSpec.cs
│           └── ActiveDestinationReviewStatsSpec.cs
├── DTOs/
│   └── DestinationDTO/
│       ├── DestinationReviewDto.cs
│       ├── DestinationReviewUpsertDto.cs
│       └── DestinationReviewPageDto.cs
├── Services/
│   └── Destinations/
│       └── DestinationService.cs            # Implement review flows + aggregate refresh
└── Validators/
    └── DestinationReviewUpsertValidator.cs

Travio.Infrastructure/
├── ApplicationDbContext.cs                  # Add DbSet<DestinationReview>
├── Configrations/
│   └── Destinations/
│       └── DestinationReviewConfiguration.cs
└── Migrations/
    └── <new migration files>
```

**Structure Decision**: Use the existing 3-project layered architecture (API/Core/Infrastructure). Add review domain model and business logic in `Travio.Core`, persistence mapping in `Travio.Infrastructure`, and HTTP surface in `Travio.API` under the existing destinations controller.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No constitution violations identified.
