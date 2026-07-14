<p align="center">
  <img src="https://res.cloudinary.com/dn8tsma3t/image/upload/v1773413577/travlo22_b5j7mo.png" alt="Travio Logo" width="120" />
</p>

<h1 align="center">Travio — Travel Companion Platform</h1>

<p align="center">
  <strong>AI-Powered Trip Planning · Flight & Hotel Booking · Social Travel Community</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet" alt=".NET 9" />
  <img src="https://img.shields.io/badge/EF%20Core-9.0-512BD4?style=flat-square&logo=dotnet" alt="EF Core 9" />
  <img src="https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=flat-square&logo=microsoftsqlserver" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Stripe-Payments-635BFF?style=flat-square&logo=stripe" alt="Stripe" />
  <img src="https://img.shields.io/badge/SignalR-Real--Time-512BD4?style=flat-square" alt="SignalR" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License" />
</p>

---

## 📖 Overview

**Travio** is a comprehensive travel companion platform that transforms how travelers plan, book, and share their journeys. The backend is a RESTful API built with ASP.NET Core 9 following **Clean Architecture** principles, designed to serve mobile (Flutter) and web clients.

The platform combines **AI-driven itinerary generation**, real-time flight and hotel booking through industry-leading providers, and a social community where travelers can share their experiences — all in a single, unified backend.

---

## 🎯 Business Context

### The Problem

Planning a trip today is fragmented — travelers juggle between dozens of apps and websites for inspiration, itinerary planning, flight search, hotel booking, and sharing experiences. There's no single platform that ties these together intelligently.

### The Solution

Travio provides an **end-to-end travel experience**:

| Stage | Feature | Description |
|-------|---------|-------------|
| 🔍 **Discover** | Destination Explorer | Browse 1,000+ curated destinations with ratings, reviews, and location-based suggestions |
| 🤖 **Plan** | AI Trip Planner | Chat with an AI assistant that generates multi-day itineraries with activities, restaurants, and hotels |
| ✈️ **Book Flights** | Duffel Integration | Search and book real flights across global airlines with live pricing |
| 🏨 **Book Hotels** | Hotelbeds APITUDE | Search, compare rates, and book hotels from 180,000+ properties worldwide |
| 💳 **Pay** | Stripe Checkout | Secure payment processing with Stripe PaymentIntents and webhook confirmation |
| 🌍 **Share** | Social Community | Post travel stories, share photos, like and comment on fellow travelers' content |
| 📋 **Manage** | Trip Management | Save AI-generated plans, favorite trips, manage bookings, and track travel history |

---

## 🏗️ Architecture

The solution follows **Clean Architecture** (also known as Onion Architecture), ensuring separation of concerns and testability:

```
Travio.Solution/
├── Travio.API                 # Presentation Layer — Controllers, Middleware, Hubs
│   ├── Controllers/           # REST API endpoints (14 controllers)
│   ├── Hubs/                  # SignalR real-time communication
│   ├── Middleware/             # Global exception handling
│   ├── Filters/               # Action filters (endpoint logging)
│   ├── Extensions/            # Service registration extensions
│   └── OpenApiTransformers/   # Swagger/OpenAPI customization
│
├── Travio.Core                # Application & Domain Layer — Business logic
│   ├── Domain/                # Entities, Enums, Specifications
│   │   ├── Entities/          # Account, Community, Destinations, Duffel, Hotelbeds, TripPlaner
│   │   ├── Enums/             # Business enumerations
│   │   └── Specifications/    # Ardalis Specification pattern queries
│   ├── Contracts/             # Service interfaces (Dependency Inversion)
│   ├── Services/              # Business logic implementations
│   ├── DTOs/                  # Data Transfer Objects per module
│   ├── Validators/            # FluentValidation rules
│   ├── MappingProfiles/       # Mapster object mapping profiles
│   ├── Helpers/               # Pagination, claims extraction utilities
│   └── Setting/               # Configuration POCOs (JWT, Email, etc.)
│
├── Travio.Infrastructure      # Infrastructure Layer — Data access, seeding
│   ├── ApplicationDbContext    # EF Core DbContext
│   ├── Configurations/        # Entity type configurations (Fluent API)
│   ├── Repositories/          # Generic Repository implementation
│   ├── Migrations/            # EF Core migrations
│   ├── Jobs/                  # Hangfire background jobs
│   └── *Seed.cs               # Data seeders (Destinations, Cities, Reviews, Identity)
```

### Key Design Patterns

| Pattern | Usage |
|---------|-------|
| **Repository Pattern** | Generic repository with Ardalis Specification for type-safe queries |
| **Specification Pattern** | Encapsulates query logic into reusable, composable specification objects |
| **Service Layer** | Business logic encapsulated behind interfaces for DI and testability |
| **DTO Pattern** | Separate request/response models per module to decouple API contracts from entities |
| **Middleware Pipeline** | Global exception handling with structured error responses |

---

## 🧩 Modules

### 1. Authentication & Authorization

Full identity management built on ASP.NET Core Identity with JWT Bearer tokens.

- **Registration & Login** — Email/password and Google OAuth 2.0
- **JWT Token Management** — Access tokens with configurable expiration + refresh token rotation
- **Email Verification** — OTP-based email confirmation via SMTP (MailKit)
- **Password Recovery** — Forgot password → OTP verification → secure reset flow
- **Role-Based Access** — Admin and User roles with seeded admin account

### 2. Destination Discovery

Explore a rich catalog of curated travel destinations.

- **Browse & Filter** — By city, country, interest, with pagination and sorting (by rating)
- **Search** — Full-text keyword search with interest-based filtering
- **Nearby** — Location-aware suggestions using latitude/longitude and radius
- **Top Rated** — Algorithmically ranked destinations
- **Suggested** — Content-based recommendations from a given destination
- **Famous Countries** — Highlighted country cards with flag imagery
- **Reviews** — Full CRUD for destination reviews with per-user ownership

### 3. AI Trip Planner

An intelligent conversational trip planner powered by an AI assistant.

- **Real-Time Chat** — SignalR WebSocket hub with typewriter-style streaming responses
- **Itinerary Generation** — Multi-day plans with themed days, timed activities, and hotel recommendations
- **Auto-Save** — Completed itineraries automatically persisted with days, activities, and hotels
- **Chat History** — Paginated session management with full message retrieval
- **Status Polling** — Background polling for long-running AI plan generation

### 4. Flight Booking (Duffel)

End-to-end flight search and booking via the [Duffel API](https://duffel.com/).

- **Flight Search** — Multi-city, one-way, and round-trip searches
- **Top Offers** — Curated best-deal flights
- **Offer Details** — Full flight breakdown with segments, durations, and baggage info
- **Checkout** — Stripe-integrated payment flow with passenger details

### 5. Hotel Booking (Hotelbeds APITUDE)

Comprehensive hotel booking through the [Hotelbeds APITUDE API](https://developer.hotelbeds.com/).

- **Destination Autocomplete** — Search destinations by keyword
- **Availability Search** — Room availability with occupancy configuration
- **Hotel Details** — Full property info with images, facilities, and room types
- **Rate Check** — Real-time price confirmation before booking
- **Checkout** — Stripe PaymentIntent initialization
- **Booking Management** — View bookings, get live details, cancel with refund status

### 6. Hotel Booking (Duffel Stays)

Alternative hotel search and details via the Duffel Stays API.

- **Hotel Search** — Search by location, dates, and guest count
- **Hotel Details** — Property info with room rates

### 7. Payments (Stripe)

Secure payment processing with full webhook lifecycle.

- **PaymentIntent Creation** — Server-side intent initialization for flights and hotels
- **Webhook Processing** — Listens for `payment_intent.succeeded` events to confirm bookings
- **Signature Verification** — Stripe signature validation for webhook security

### 8. Social Community

A travel-focused social feed for sharing experiences.

- **Posts** — Create text posts, attach multiple images (cloud-hosted)
- **Feed** — Paginated community feed
- **Engagement** — Like/unlike toggle, comments with full CRUD
- **Post Details** — Single post view with comments and engagement metrics
- **Ownership** — Users can only edit/delete their own content

### 9. User Profile

Personalization and account management.

- **Profile Management** — Update display name, bio, and preferences
- **Profile Image** — Upload and store profile photos
- **Onboarding Survey** — Interest-based onboarding questionnaire for personalized recommendations

### 10. Favorites

Cross-module bookmarking system.

- **Destination Favorites** — Add/remove destinations from favorites with paginated retrieval
- **Trip Favorites** — Favorite saved trip plans for quick access

---

## 🔌 API Endpoints

### Authentication (`/api/Auth`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/register` | Create a new account |
| `POST` | `/login` | Authenticate and receive JWT |
| `POST` | `/google-login` | Login via Google OAuth |
| `POST` | `/refreshToken` | Rotate access token |
| `POST` | `/Logout` | Revoke refresh token |
| `POST` | `/forgot-password` | Initiate password reset |
| `POST` | `/send-verify-email-otp` | Send email verification OTP |
| `POST` | `/verify-email` | Confirm email with OTP |
| `POST` | `/verify-reset-password-otp` | Verify reset password OTP |
| `POST` | `/reset-password` | Set new password |

### Destinations (`/api/Destinations`) 🔒

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | Browse all destinations (paginated, filterable) |
| `GET` | `/{id}` | Get destination details |
| `GET` | `/{id}/suggested` | Get similar destinations |
| `GET` | `/top-rated` | Get top-rated destinations |
| `GET` | `/search` | Search by keyword and interests |
| `GET` | `/nearby` | Find destinations near a location |
| `GET` | `/famous-countries` | Get featured countries |
| `GET` | `/{id}/reviews` | Get destination reviews (paginated) |
| `POST` | `/{id}/reviews` | Add or update a review |
| `PUT` | `/{id}/reviews/me` | Update your review |
| `DELETE` | `/{id}/reviews/me` | Delete your review |

### AI Trip Planner (`/api/Ai`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/chat` | Send a message to the AI planner |
| `GET` | `/status/{threadId}` | Check itinerary generation status |

### Chat History (`/api/chat`) 🔒

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/sessions` | Get user's chat sessions (paginated) |
| `GET` | `/sessions/{id}/messages` | Get session messages (paginated) |
| `DELETE` | `/sessions/{id}` | Delete a chat session |

### Saved Trips (`/api/Trip`) 🔒

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | Get user's saved trips (paginated) |
| `GET` | `/favorites` | Get favorite trips (paginated) |
| `GET` | `/{id}` | Get full trip details |
| `POST` | `/{id}/favorite` | Toggle trip favorite |
| `DELETE` | `/{id}` | Delete a saved trip |

### Flight Booking (`/api/FlightBooking`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/flights/search` | Search available flights |
| `GET` | `/top-offers` | Get curated top flight deals |
| `GET` | `/{offerId}` | Get flight offer details |
| `POST` | `/checkout` 🔒 | Initiate flight checkout |

### Hotels — Hotelbeds (`/api/Hotels`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/destinations/search` | Autocomplete destination search |
| `POST` | `/search` | Search hotel availability |
| `GET` | `/{hotelCode}/details` | Get hotel details |
| `POST` | `/check-rate` | Confirm room rate |
| `POST` | `/checkout` 🔒 | Initiate hotel booking |
| `GET` | `/my-bookings` 🔒 | List user's bookings |
| `GET` | `/bookings/{reference}` 🔒 | Get booking details |
| `DELETE` | `/bookings/{reference}` 🔒 | Cancel a booking |

### Hotels — Duffel (`/api/DuffelHotels`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/search` | Search hotels |
| `GET` | `/details/{searchResultId}` | Get hotel details |

### Community (`/api/Community`) 🔒

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/create-post` | Create a new post |
| `GET` | `/feed` | Get community feed (paginated) |
| `GET` | `/posts/{postId}` | Get post details |
| `DELETE` | `/posts/{postId}` | Delete a post |
| `POST` | `/posts/{postId}/images` | Upload post images |
| `POST` | `/posts/{postId}/toggle-like` | Like/unlike a post |
| `POST` | `/posts/{postId}/comments` | Add a comment |
| `DELETE` | `/comments/{commentId}` | Delete a comment |

### Favorites (`/api/Favorites`) 🔒

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/{destinationId}` | Add destination to favorites |
| `DELETE` | `/{destinationId}` | Remove from favorites |
| `GET` | `/` | Get user's favorite destinations (paginated) |

### Profile (`/api/Profile`) 🔒

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | Get user profile |
| `PUT` | `/update-profile` | Update profile info |
| `POST` | `/upload-image` | Upload profile picture |

### Survey (`/api/Survey`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/onboarding` | Get onboarding survey questions |
| `POST` | `/user-preferences` 🔒 | Submit survey answers |

### Payments (`/api/Stripe`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/webhook` | Stripe webhook receiver |

> 🔒 = Requires JWT Bearer token

---

## 🛠️ Tech Stack

| Category | Technology |
|----------|------------|
| **Runtime** | .NET 9.0 |
| **Framework** | ASP.NET Core Web API |
| **Database** | SQL Server + Entity Framework Core 9 |
| **Authentication** | ASP.NET Core Identity + JWT Bearer + Google OAuth 2.0 |
| **Real-Time** | SignalR WebSockets |
| **Object Mapping** | Mapster |
| **Validation** | FluentValidation |
| **Payments** | Stripe.net |
| **Flight Booking** | Duffel API Client |
| **Hotel Booking** | Hotelbeds APITUDE API |
| **Email** | MailKit (SMTP) |
| **Background Jobs** | Hangfire (SQL Server storage) |
| **Logging** | Serilog (Console + File sinks) |
| **API Documentation** | OpenAPI 3.0 + Scalar UI |
| **Query Patterns** | Ardalis Specification |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/sql-server) (local or remote)
- [Stripe CLI](https://stripe.com/docs/stripe-cli) (optional, for webhook testing)

### 1. Clone the Repository

```bash
git clone https://github.com/MahmoudAmin5/Travio.git
cd Travio.Solution
```

### 2. Configure Secrets

Update `appsettings.json` or use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for sensitive values:

```bash
dotnet user-secrets init --project Travio.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Your_Connection_String" --project Travio.API
dotnet user-secrets set "JWTSetting:Key" "Your_JWT_Secret_Key" --project Travio.API
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project Travio.API
dotnet user-secrets set "Duffel:AccessToken" "duffel_test_..." --project Travio.API
```

### 3. Apply Migrations

```bash
dotnet ef database update --project Travio.Infrastructure --startup-project Travio.API
```

### 4. Run the Application

```bash
dotnet run --project Travio.API
```

The API will start at `https://localhost:5001` (or the port configured in `launchSettings.json`).

### 5. Explore the API

Navigate to the Scalar API documentation:

```
https://localhost:5001/scalar/v1
```

### 6. Test Stripe Webhooks (Optional)

```bash
stripe listen --forward-to https://localhost:5001/api/Stripe/webhook
```

---

## 🗄️ Database

The application uses **Entity Framework Core** with **Code-First** migrations. The database is automatically migrated and seeded on startup via `ApplyMigrationsAndSeedAsync()`.

### Seeded Data

| Seed | Description |
|------|-------------|
| **Identity** | Admin user with pre-configured roles |
| **World Cities** | Countries, continents, and cities hierarchy |
| **Destinations** | 1,000+ travel destinations with images and metadata |
| **Reviews** | Sample destination reviews |
| **Hotel Destinations** | Hotelbeds destination cache for autocomplete |

---

## 📡 Real-Time Communication

The **TripPlanerHub** SignalR hub powers the AI chat experience:

| Event | Direction | Description |
|-------|-----------|-------------|
| `SendMessage` | Client → Server | Send user message with thread ID |
| `ReceiveMessageChunk` | Server → Client | Streamed AI response (typewriter effect) |
| `ReceiveStatus` | Server → Client | AI status updates (`thinking`) |
| `MessageComplete` | Server → Client | Full response metadata |
| `ReceiveItineraryStatus` | Server → Client | Completed itinerary data |
| `TripSaved` | Server → Client | Notification that trip was auto-saved |
| `ReceiveError` | Server → Client | Error messages |
| `ReceiveSystemMessage` | Server → Client | System notifications |

**Connection URL:** `wss://{host}/hubs/trip-planer` (requires JWT in query string)

---

## 🔧 Background Jobs

Hangfire manages scheduled and recurring tasks:

- **OTP Cleanup** — Periodically removes expired verification codes
- **Dashboard** — Accessible at `/hangfire` with basic authentication

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Built with ❤️ by the <strong>Travio Team</strong>
</p>
