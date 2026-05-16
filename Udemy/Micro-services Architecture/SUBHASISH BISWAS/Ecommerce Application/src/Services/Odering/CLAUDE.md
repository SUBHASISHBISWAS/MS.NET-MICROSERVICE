# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

The Ordering microservice is part of a larger e-commerce microservices architecture. It handles order creation, updates, and queries using Clean Architecture, Domain-Driven Design (DDD), and CQRS patterns.

## Architecture

The service follows a **4-layer Clean Architecture**:

1. **Ordering.Domain** - Core business logic, entities, value objects, domain events (no dependencies)
2. **Ordering.Application** - CQRS handlers, validators, DTOs, interfaces (depends on Domain)
3. **Ordering.Infrastructure** - Data persistence, repositories, external services (depends on Application)
4. **Ordering.API** - REST endpoints, integration events, API configuration (depends on Application & Infrastructure)

### CQRS Implementation

- **Commands**: Write operations handled via MediatR command handlers (`CheckoutOrder`, `UpdateOrder`, `DeleteOrder`)
- **Queries**: Read operations handled via MediatR query handlers (`GetOrdersList`)
- **Validation**: FluentValidation runs automatically via MediatR pipeline behaviors before command execution
- **Mapping**: AutoMapper handles entity ↔ DTO conversions

### Event-Driven Integration

- **MassTransit** abstracts message bus operations
- **RabbitMQ** as the message broker
- **BasketCheckoutConsumer** listens to `BasketCheckoutQueue` and creates orders from basket checkout events
- Integration events flow: `Basket.API` → RabbitMQ → `Ordering.API.BasketCheckoutConsumer` → `CheckoutOrderCommandHandler`

### Domain-Driven Design

The Domain layer contains:
- **EntityBase**: Base class with audit fields (`CreatedBy`, `CreatedDate`, `LastModifiedBy`, `LastModifiedDate`)
- **ValueObject**: Abstract base for value objects (Address, Payment, strongly-typed IDs)
- **Order Entity**: Aggregate root with business rules
- **Domain Events**: `OrderCreatedEvent`, `OrderUpdatedEvent`

### Event Sourcing (Optional Advanced Implementation)

An alternative Event Sourcing implementation exists alongside the traditional CRUD approach:
- **OrderES**: Event-sourced aggregate that rebuilds state from event stream
- **CosmosDbEventStore**: Persists events to Azure CosmosDB with optimistic concurrency
- **Domain Events**: `OrderCreatedEventES`, `OrderUpdatedEventES`, `OrderItemAddedEvent`, `OrderItemRemovedEvent`
- Events are append-only and provide full audit history

## Common Commands

### Build & Run

```bash
# Build the Ordering service
dotnet build Ordering.API/Ordering.API.csproj

# Run the API locally
dotnet run --project Ordering.API/Ordering.API.csproj

# Restore NuGet packages
dotnet restore
```

### Database Migrations

```bash
# Add new migration (run from Ordering.API directory)
dotnet ef migrations add MigrationName --project ../Ordering.Infrastructure/Ordering.Infrastructure.csproj --startup-project Ordering.API.csproj

# Apply migrations manually (migrations auto-run on startup via HostExtension.MigrateDatabase)
dotnet ef database update --project ../Ordering.Infrastructure/Ordering.Infrastructure.csproj --startup-project Ordering.API.csproj

# Remove last migration
dotnet ef migrations remove --project ../Ordering.Infrastructure/Ordering.Infrastructure.csproj --startup-project Ordering.API.csproj
```

### Docker

```bash
# Build and run entire microservices stack (from src/ directory)
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Build and run only Ordering service and dependencies
docker-compose up orderdb rabbitmq ordering.api

# View logs
docker-compose logs -f ordering.api

# Rebuild specific service
docker-compose build ordering.api
```

### Testing

```bash
# Run all tests (when test projects exist)
dotnet test

# Run tests for specific project
dotnet test Ordering.Application.Tests/Ordering.Application.Tests.csproj
```

## Service Dependencies

- **SQL Server** (orderdb) - Port 1433
  - Database: OrderDb
  - Connection string in `appsettings.json` and docker-compose override
  - Migrations auto-apply on startup with retry logic (50 retries, 2s delay)

- **RabbitMQ** (rabbitmq) - Ports 5672 (AMQP), 15672 (Management UI)
  - Message broker for async communication
  - Management UI: http://localhost:15672 (guest/guest)
  - Queue: `BasketCheckoutQueue`

- **SendGrid** - Email service
  - API key configured in `appsettings.json` under `EmailSettings:ApiKey`

## API Endpoints

Base URL when running locally: http://localhost:8004

- **GET** `/api/v1/Order/{userName}` - Get orders by username
- **POST** `/api/v1/Order` - Create new order (checkout)
- **PUT** `/api/v1/Order` - Update existing order
- **DELETE** `/api/v1/Order/{id}` - Delete order by ID

Swagger UI available at: http://localhost:8004/swagger

## Configuration

Key configuration in `appsettings.json`:

- **OrderingConnectionString**: SQL Server connection string
- **EmailSettings**: SendGrid API key and sender details
- **EventBusSettings:HostAddress**: RabbitMQ connection URL

Docker environment variables in `docker-compose.override.yml` override local settings.

## Key Design Patterns

- **Repository Pattern**: `IAsyncRepository<T>`, `IOrderRepository` with generic CRUD operations
- **Mediator Pattern**: MediatR decouples request/response handling
- **Pipeline Behaviors**: Cross-cutting concerns (validation, exception handling) run before handlers
- **Factory Pattern**: Domain entities use static `Create()` methods for instantiation
- **Value Objects**: Strongly-typed IDs prevent primitive obsession (OrderId, CustomerId, ProductId)

## Data Flow

### Command Flow (Write)
API Controller → MediatR → ValidationBehaviour → CommandHandler → Repository → OrderContext → SQL Server

### Query Flow (Read)
API Controller → MediatR → QueryHandler → Repository (AsNoTracking) → AutoMapper → ViewModel

### Integration Event Flow
Basket.API → RabbitMQ (BasketCheckoutQueue) → BasketCheckoutConsumer → MediatR → CheckoutOrderCommandHandler → Repository

## Project Structure

```
Ordering/
├── Ordering.API/               # REST API layer
│   ├── Controllers/            # API endpoints
│   ├── EventBusConsumer/       # MassTransit consumers
│   ├── Extensions/             # HostExtension for DB migration
│   └── Mapping/                # AutoMapper profiles
├── Ordering.Application/       # Application logic (CQRS)
│   ├── Commands/               # Command handlers
│   ├── Queries/                # Query handlers
│   ├── Behaviours/             # MediatR pipeline behaviors
│   ├── Contracts/              # Interfaces (repositories, services)
│   ├── Models/                 # DTOs and ViewModels
│   └── Exceptions/             # Custom exceptions
├── Ordering.Domain/            # Domain layer (DDD)
│   ├── Entities/               # Domain entities (Order)
│   ├── Models/                 # Rich domain models (Order, OrderES)
│   ├── ValueObjects/           # Value objects (Address, Payment)
│   ├── Events/                 # Domain events
│   └── Common/                 # Base classes (EntityBase, ValueObject)
└── Ordering.Infrastructure/    # Infrastructure layer
    ├── Persistence/            # OrderContext, ContextSeed
    ├── Repositories/           # Repository implementations
    ├── Mail/                   # EmailService (SendGrid)
    └── EventStore/             # CosmosDbEventStore (Event Sourcing)
```

## Important Implementation Details

### Automatic Database Migration
The `Program.Main()` method calls `host.MigrateDatabase<OrderContext>()` before `host.Run()`, which:
- Automatically applies pending EF Core migrations on startup
- Seeds initial data via `OrderContextSeed.SeedAsync()`
- Includes retry logic for SQL connection failures (50 retries, 2s intervals)

### Audit Trail
`OrderContext.SaveChangesAsync()` automatically populates audit fields:
- On insert: Sets `CreatedDate` and `CreatedBy` (currently hardcoded to "subhasish")
- On update: Sets `LastModifiedDate` and `LastModifiedBy`

### Exception Handling
- **ValidationException**: Thrown by ValidationBehaviour when FluentValidation fails
- **NotFoundException**: Thrown by command handlers when entity not found
- **UnhandledExceptionBehaviour**: Logs all unhandled exceptions

### Email Notifications
`CheckoutOrderCommandHandler` sends confirmation emails via `IEmailService` after successful order creation.

## Technology Stack

- **.NET 5.0** - Target framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core 5.0.17** - ORM
- **SQL Server 2019** - Relational database
- **MediatR 10.0.1** - CQRS mediator
- **AutoMapper 11.0.1** - Object mapping
- **FluentValidation 11.0.2** - Input validation
- **MassTransit 8.0.3** - Message bus abstraction
- **RabbitMQ** - Message broker
- **SendGrid 9.28.0** - Email service
- **Swashbuckle 5.6.3** - OpenAPI/Swagger
- **Azure CosmosDB** - Event store (Event Sourcing implementation)

## Development Notes

### Adding New Commands
1. Create command class in `Ordering.Application/Commands/[Feature]/`
2. Create command handler implementing `IRequestHandler<TCommand, TResponse>`
3. Create validator inheriting `AbstractValidator<TCommand>` (optional but recommended)
4. Handler automatically gets validation via `ValidationBehaviour`

### Adding New Queries
1. Create query class in `Ordering.Application/Queries/[Feature]/`
2. Create query handler implementing `IRequestHandler<TQuery, TResponse>`
3. Create ViewModel/DTO for response
4. Add AutoMapper mapping profile

### Adding New Domain Events
1. Create event class in `Ordering.Domain/Events/` (for traditional) or with `ES` suffix (for Event Sourcing)
2. Raise event in domain entity business method
3. Create event handler in `Ordering.Application/Orders/EventHandlers/`

### Integration Events
- Integration events flow between microservices via MassTransit/RabbitMQ
- Define contracts in `BuildingBlocks.Messaging.Events` (shared across services)
- Create consumer implementing `IConsumer<TEvent>` in `Ordering.API/EventBusConsumer/`
- Register consumer in `Startup.ConfigureServices()` via `cfg.AddConsumer<TConsumer>()`

## Connection Information

When running via Docker Compose:

- **Ordering API**: http://localhost:8004
- **Swagger UI**: http://localhost:8004/swagger
- **SQL Server**: localhost:1433 (sa/SqlServer@2019)
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Portainer**: http://localhost:9000
- **API Gateway (Ocelot)**: http://localhost:8010
- **Shopping Aggregator**: http://localhost:8005
- **Frontend (AspnetRunBasics)**: http://localhost:8006
