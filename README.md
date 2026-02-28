# EventFlow

> **Production-style distributed system** built on **.NET 10** with Microservices + Event-Driven Architecture.

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 (LTS) |
| Web API | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 + Npgsql 10 |
| Message Broker | RabbitMQ 4 via MassTransit 8.5.5 |
| Caching / Rate Limiting | Redis 7 |
| Auth | JWT Bearer + BCrypt + Refresh Token Rotation |
| API Gateway | YARP Reverse Proxy |
| Validation | FluentValidation 11 |
| Logging | Serilog (structured JSON) |
| Tracing | OpenTelemetry |
| Containers | Docker + Docker Compose |
| Tests | xUnit + Moq + FluentAssertions + Testcontainers |

## Services

```
EventFlow.GatewayApi          → Port 8080  (public entry point)
EventFlow.AuthService         → Port 5001  (internal)
EventFlow.CoreService         → Port 5002  (internal)
EventFlow.WorkerService       → Background worker
EventFlow.NotificationService → Background worker
EventFlow.SharedKernel        → Shared library (DTOs, messages)
```

## Quick Start

### Prerequisites
- Docker Desktop
- .NET 10 SDK
- Visual Studio 2022 (17.12+) or VS Code

### Run with Docker

```bash
cd docker
docker compose up --build
```

| Service | URL |
|---|---|
| API Gateway | http://localhost:8080 |
| Auth Service Swagger | http://localhost:5001/swagger |
| Core Service Swagger | http://localhost:5002/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |

### Run Locally (without Docker)

1. Start infrastructure:
```bash
cd docker
docker compose up postgres_auth postgres_core postgres_worker postgres_notification rabbitmq redis -d
```

2. Start each service:
```bash
# Auth Service
cd auth-service/EventFlow.AuthService
dotnet run

# Core Service (new terminal)
cd core-service/EventFlow.CoreService
dotnet run

# Worker Service (new terminal)
cd worker-service/EventFlow.WorkerService
dotnet run

# Notification Service (new terminal)
cd notification-service/EventFlow.NotificationService
dotnet run

# Gateway (new terminal)
cd gateway-api/EventFlow.GatewayApi
dotnet run
```

### Run Tests

```bash
dotnet test EventFlow.sln
```

## API Flow

```
Client → Gateway (8080) → Auth Service: POST /api/auth/register
Client → Gateway (8080) → Auth Service: POST /api/auth/login       → JWT + RefreshToken
Client → Gateway (8080) → Core Service: POST /api/events           → Event created
                                              ↓ RabbitMQ
                                    Worker Service (analytics)
                                    Notification Service (email log)
```

## Environment Variables

Each service loads its own `.env` file. Key variables:

| Variable | Description |
|---|---|
| `POSTGRES_HOST` | PostgreSQL hostname |
| `POSTGRES_DB` | Database name |
| `JWT_SECRET` | Shared JWT signing secret (min 32 chars) |
| `RABBITMQ_HOST` | RabbitMQ hostname |
| `REDIS_HOST` | Redis hostname |
| `RUN_MIGRATIONS` | `true` to auto-apply EF Core migrations on startup |

> **Production note:** Replace `.env` values with CI/CD secrets or a vault (e.g. HashiCorp Vault, Azure Key Vault).

## Database Migrations

Each service manages its own migrations. To add a migration:

```bash
cd auth-service/EventFlow.AuthService

dotnet ef migrations add InitialCreate \
  --project EventFlow.AuthService.csproj \
  --startup-project EventFlow.AuthService.csproj
```

## Project Structure

```
EventFlow/
├── EventFlow.sln
├── shared-kernel/
│   └── EventFlow.SharedKernel/          # DTOs, messaging contracts
├── auth-service/
│   ├── EventFlow.AuthService/           # Domain + Application + Infrastructure + API
│   └── EventFlow.AuthService.Tests/
├── core-service/
│   ├── EventFlow.CoreService/
│   └── EventFlow.CoreService.Tests/
├── worker-service/
│   └── EventFlow.WorkerService/
├── notification-service/
│   └── EventFlow.NotificationService/
├── gateway-api/
│   └── EventFlow.GatewayApi/
└── docker/
    └── docker-compose.yml
```

## Architecture Highlights

- **Database per service** — zero shared schemas, no cross-service joins
- **JWT with refresh token rotation** — revoked on each use, stored in DB
- **RabbitMQ dead-letter queues** — failed messages preserved for inspection
- **Exponential retry** — MassTransit handles transient failures automatically
- **Correlation ID propagation** — every request traceable across all services
- **YARP reverse proxy** — API Gateway routes via config (zero custom code for routing)
- **Rate limiting** — Redis sliding window per IP
