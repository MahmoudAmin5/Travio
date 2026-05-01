# 🌍 Travio - AI-Powered Travel & Booking API

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-ff69b4?style=for-the-badge)

Travio is a smart travel recommendation, booking, and social platform. This repository contains the robust backend API built to support the Travio mobile application, serving as a graduation project. It handles user authentication, flight/hotel bookings, destination exploration, community interactions, and AI-generated personalized itineraries.

## ✨ Key Features

* **🔐 Authentication & Authorization:** Secure user registration and login using JWT (JSON Web Tokens) with role-based access control.
* **📍 Destinations & Attractions:** Explore and search for detailed information about various cities, landmarks, and tourist spots.
* **✈️ Flights & Hotels Booking:** Comprehensive modules to search, filter, and manage flight and accommodation reservations.
* **👥 Traveler Community:** A social hub for users to share travel experiences, post reviews, and interact with other travelers' itineraries.
* **🤖 AI-Driven Itineraries:** Integrates with an external AI microservice via an asynchronous polling mechanism to generate smart, day-by-day travel plans based on user budget, interests, and companions.
* **💬 Interactive AI Chat:** A multi-turn chat interface to accurately collect user trip constraints before generating the final travel plan.

## 🏗️ Architecture & Tech Stack

This project strictly adheres to **Clean Architecture** (Onion Architecture) and **Domain-Driven Design (DDD)** principles to ensure high scalability, maintainability, and a clear separation of concerns.

* **Framework:** ASP.NET Core 9 Web API
* **Language:** C# 12
* **Architecture:** Clean Architecture
* **Design Patterns:** Repository Pattern for data access abstraction
* **Database:** Microsoft SQL Server
* **ORM:** Entity Framework Core
* **Security:** JWT Authentication
* **External Integration:** Typed HttpClient for seamless communication with external AI services.

## 📂 Project Structure
```text
Travio.Backend/
├── Travio.Domain/         # Enterprise logic, Entities, Enums, Exceptions
├── Travio.Application/    # Business logic, Services, DTOs, Interfaces
├── Travio.Infrastructure/ # Database Context, Repositories, External API Integrations
└── Travio.API/            # Controllers, Dependency Injection, Program.cs, Middlewares
```

## 🚀 Getting Started

### Prerequisites
* .NET 9 SDK
* SQL Server
* External AI Service running locally or accessible via URL (for the smart itinerary feature).

### Installation & Setup

**1. Clone the repository:**
```bash
git clone <YOUR_GITHUB_REPO_URL>
cd Travio
```

**2. Configure AppSettings:**
Update the `appsettings.json` in the `Travio.API` project with your database connection string, JWT secrets, and the AI service URL.
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=TravioDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False"
},
"Jwt": {
  "Key": "<YOUR_SECRET_KEY>",
  "Issuer": "<YOUR_ISSUER>",
  "Audience": "<YOUR_AUDIENCE>"
},
"AiService": {
  "BaseUrl": "<AI_SERVER_URL>"
},
```

**3. Apply Database Migrations:**
```bash
dotnet ef database update --project Travio.Infrastructure --startup-project Travio.API
```

**4. Run the Application:**
```bash
dotnet run --project Travio.API
```

## 🔌 API Documentation

Once the application is running, navigate to `/swagger` in your browser to explore the fully documented API endpoints.

### Core Modules:
* **`Auth`**: Endpoints for user registration, login, and token generation.
* **`Destinations`**: Endpoints for fetching cities, landmarks, and tourist attractions.
* **`Bookings`**: Endpoints to manage flight and hotel reservations.
* **`Community`**: Endpoints for user posts, reviews, and social interactions.
* **`AI`**: `POST /api/ai/chat` for collecting trip data, and `GET /api/ai/status/{thread_id}` for retrieving the generated itinerary.

## 🤝 Team & Contributions
This project is developed as a computer science graduation project by the backend development team:

* **[Ezzat Walid](https://github.com/EZZATW12)**
* **[Mahmoud Amin](https://github.com/MahmoudAmin5)**
````</AI_SERVER_URL></YOUR_AUDIENCE></YOUR_ISSUER></YOUR_SECRET_KEY></YOUR_GITHUB_REPO_URL>
