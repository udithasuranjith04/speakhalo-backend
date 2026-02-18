# SpeakHalo - AI-Powered Voice Call Management for Cleaning Businesses

SpeakHalo is an AI-powered voice call management platform built with .NET 8, designed exclusively for cleaning service businesses (residential, commercial, deep clean, move-in/out, carpet, post-construction).

## Features

- Real-time AI voice conversations using Google Gemini Multimodal Live API (native audio)
- Multi-channel communication (Voice, SMS, WhatsApp via Twilio)
- Appointment scheduling with Google Calendar integration
- Admin-controlled accounts (no self-service signup)
- Enhanced caller recognition (returning customers, call history, appointment context)
- AI function calling (book/reschedule/cancel appointments, save customers, check availability)
- Real-time call transcription and analysis
- Photo analysis for cleaning estimates (Gemini Vision)
- Background job processing with Hangfire
- FSM integration support (Jobber)

## Tech Stack

- **.NET 8** - ASP.NET Core Web API
- **PostgreSQL** - Production database on Azure
- **Entity Framework Core** - ORM with code-first migrations
- **Hangfire** - Background job processing
- **Redis** - Caching and session management
- **Google Gemini API** - AI conversation engine (chat, vision, native audio voice)
- **Twilio** - Voice (Media Streams WebSocket), SMS, and WhatsApp
- **SendGrid** - Email notifications
- **Google Calendar API** - Appointment integration
- **SignalR** - Real-time notifications

## Project Structure

```
SpeakHalo/
├── SpeakHalo.API/              # Web API controllers and endpoints
├── SpeakHalo.Application/      # Business logic, DTOs, validators
├── SpeakHalo.Domain/           # Domain entities and interfaces
├── SpeakHalo.Infrastructure/   # External services and data access
└── DatabaseCreator/            # Database migration utility
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL 16+
- Redis
- Google Gemini API key
- Twilio account (for voice/SMS/WhatsApp)
- SendGrid account (for emails)

## Quick Start

### 1. Configure Secrets

**Development (User Secrets):**
```bash
cd SpeakHalo.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=speakhalo_dev;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Gemini:ApiKey" "YOUR-GEMINI-API-KEY"
dotnet user-secrets list
```

**Production (Environment Variables):**
```bash
export ASPNETCORE_ENVIRONMENT="Production"
export ConnectionStrings__DefaultConnection="Host=...;Password=..."
export Jwt__SecretKey="your-64-char-secret"
export Gemini__ApiKey="your-gemini-api-key"
export Twilio__AccountSid="AC..."
export Twilio__AuthToken="..."
```

See [SECRETS_MANAGEMENT.md](./docs/SECRETS_MANAGEMENT.md) for complete guide.

### 2. Run Database Migrations

```bash
cd DatabaseCreator
export DATABASE_HOST="your-host"
export DATABASE_PORT="5432"
export DATABASE_USER="your-user"
export DATABASE_PASSWORD="your-password"
dotnet run
```

### 3. Start the Application

```bash
cd SpeakHalo.API
dotnet run
```

## Architecture

Clean Architecture with 4 layers:
1. **Domain** - Entities, interfaces, enums (no dependencies)
2. **Application** - Business logic, DTOs, validators, service interfaces
3. **Infrastructure** - External services, EF Core, repositories
4. **API** - Controllers, middleware, configuration

## Security

- JWT authentication with refresh tokens (SHA-256 hashed)
- BCrypt password hashing (work factor 12)
- 2FA support via authenticator apps (TOTP, AES-encrypted secrets)
- Account lockout (5 failures / 15 minutes)
- Anti-enumeration on auth endpoints
- Twilio webhook signature validation
- Rate limiting on all endpoints
- Input sanitization (XSS, SQL injection protection)
- IDOR protection across all controllers
- Feature gating and usage enforcement

## Admin System

Admin controls all user accounts:
- Create users with temporary password (sent via email)
- Set custom usage limits (AI minutes, SMS, WhatsApp, images)
- Suspend/activate accounts
- Reset passwords
- Reset usage counters
- View analytics and dashboards

## Documentation

- [Production Setup Guide](./docs/PRODUCTION_SETUP.md)
- [Secrets Management](./docs/SECRETS_MANAGEMENT.md)
- [Twilio Phone Numbers](./docs/TWILIO_PHONE_NUMBERS.md)
- [API Documentation](./docs/API_DOCUMENTATION.md)

## License

Proprietary - All Rights Reserved
