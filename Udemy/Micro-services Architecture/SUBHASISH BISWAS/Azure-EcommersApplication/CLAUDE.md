# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a production-ready .NET 8 microservices e-commerce application demonstrating modern architectural patterns including DDD, CQRS, Event-Driven Architecture, and Clean Architecture. The system comprises independent services for Catalog, Basket, Discount, and Ordering, communicating via both synchronous (gRPC) and asynchronous (Azure Service Bus) protocols.

## Architecture Patterns

### Microservices Structure
- **Catalog** - Product catalog management using Vertical Slice Architecture with Marten (PostgreSQL document DB)
- **Basket** - Shopping cart with Repository + Decorator pattern, Redis caching, gRPC client to Discount service
- **Discount** - gRPC server using EF Core with SQLite
- **Ordering** - Full DDD implementation with Clean Architecture (Domain/Application/Infrastructure/API layers), consumes Azure Service Bus events
- **YarpApiGateway** - Reverse proxy with rate limiting (5 req/10 sec on ordering)
- **Shopping.Web** - ASP.NET Razor Pages UI calling APIs via Refit

### Key Patterns
- **CQRS**: Commands (ICommand) and Queries (IQuery) with MediatR mediation in Catalog, Basket, Ordering
- **DDD**: Ordering service uses Aggregate Root (Order), Value Objects (OrderId, Address, Payment), Domain Events (OrderCreatedEvent)
- **Event-Driven**: Basket publishes BasketCheckoutEvent → Azure Service Bus → Ordering consumes via MassTransit
- **Vertical Slices**: Catalog organizes by feature (CreateProduct/, UpdateProduct/) with all layers in single folder
- **Clean Architecture**: Ordering separates Domain → Application → Infrastructure → API
- **Decorator Pattern**: CachedBasketRepository wraps IBasketRepository for transparent caching

### Communication
- **Synchronous**: Basket → Discount.Grpc for discount calculations
- **Asynchronous**: Basket publishes BasketCheckoutEvent, Ordering consumes it
- **API Gateway**: All external requests route through Yarp

## Development Commands

### Prerequisites
- .NET 8 SDK
- Docker Desktop (must be running)
- Docker memory: 4GB minimum, CPU: 2 cores
- Azure Service Bus namespace (see Azure Service Bus Setup below)

### Azure Service Bus Setup

Before running the application, you need to create an Azure Service Bus namespace:

1. Create Azure Service Bus namespace in Azure Portal
2. Get the connection string from "Shared access policies" → "RootManageSharedAccessKey"
3. Update the connection string in:
   - `src/Services/Basket/Basket.API/appsettings.json`
   - `src/Services/Ordering/Ordering.API/appsettings.json`
   - `src/docker-compose.override.yml` (for Docker deployment)

Connection string format:
```
Endpoint=sb://<your-namespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<your-key>
```

### Building

```bash
# Build entire solution
cd src
dotnet build eshop-microservices.sln

# Build specific service
dotnet build Services/Catalog/Catalog.API/Catalog.API.csproj
dotnet build Services/Basket/Basket.API/Basket.API.csproj
dotnet build Services/Discount/Discount.Grpc/Discount.Grpc.csproj
dotnet build Services/Ordering/Ordering.API/Ordering.API.csproj

# Build with configuration
dotnet build -c Release
```

### Running with Docker Compose (Recommended)

```bash
# From src/ directory (contains docker-compose files)
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# View logs
docker-compose logs -f [service-name]

# Stop services
docker-compose down

# Rebuild and restart specific service
docker-compose up -d --build catalog.api
```

### Running Individual Services Locally

```bash
# Run Catalog API
cd src/Services/Catalog/Catalog.API
dotnet run

# Run Basket API
cd src/Services/Basket/Basket.API
dotnet run

# Run Discount gRPC
cd src/Services/Discount/Discount.Grpc
dotnet run

# Run Ordering API
cd src/Services/Ordering/Ordering.API
dotnet run

# Run API Gateway
cd src/ApiGateways/YarpApiGateway
dotnet run

# Run Web UI
cd src/WebApps/Shopping.Web
dotnet run
```

### Testing
Note: This repository does not include test projects. When writing tests, follow these patterns:
- Unit tests for command/query handlers
- Integration tests for API endpoints
- Use xUnit as testing framework (consistent with .NET ecosystem)

### Database Migrations (Ordering Service)

```bash
# Add new migration
cd src/Services/Ordering/Ordering.API
dotnet ef migrations add MigrationName -p ../Ordering.Infrastructure/Ordering.Infrastructure.csproj -s Ordering.API.csproj

# Apply migrations (runs automatically on startup, but can be done manually)
dotnet ef database update -p ../Ordering.Infrastructure/Ordering.Infrastructure.csproj -s Ordering.API.csproj
```

## Code Organization

### Solution Structure
- **src/Services/** - Microservices (Catalog.API, Basket.API, Discount.Grpc, Ordering.*)
- **src/BuildingBlocks/** - Shared libraries (CQRS interfaces, validation, messaging)
- **src/ApiGateways/** - Yarp reverse proxy
- **src/WebApps/** - Shopping.Web UI

### BuildingBlocks (Shared Libraries)
- **BuildingBlocks.csproj**: CQRS interfaces (ICommand/IQuery/ICommandHandler/IQueryHandler), ValidationBehavior, LoggingBehavior, CustomExceptionHandler, Pagination
- **BuildingBlocks.Messaging.csproj**: IntegrationEvent base class, BasketCheckoutEvent, MassTransit/Azure Service Bus configuration extensions

### Vertical Slice Architecture (Catalog)
Features organized in single folders with all layers together:
```
Products/
├── CreateProduct/
│   ├── CreateProductCommand.cs (record + validator)
│   ├── CreateProductHandler.cs
│   └── CreateProductEndpoint.cs (ICarterModule)
```

### DDD Layers (Ordering)
```
Ordering.Domain/         # Aggregates, Entities, Value Objects, Domain Events
Ordering.Application/    # Commands, Queries, Handlers, DTOs
Ordering.Infrastructure/ # EF Core, Repositories, Interceptors
Ordering.API/           # Endpoints (Carter modules)
```

## Important Implementation Details

### CQRS Implementation
- Commands/Queries extend BuildingBlocks interfaces (ICommand<T>, IQuery<T>)
- MediatR dispatches to handlers
- ValidationBehavior runs FluentValidation before handler execution
- LoggingBehavior logs all requests

### Domain Events (Ordering)
- Order aggregate raises OrderCreatedEvent, OrderUpdatedEvent
- DispatchDomainEventsInterceptor in SaveChangesAsync publishes events via MediatR
- Handlers process events in-process

### Integration Events
- Basket publishes BasketCheckoutEvent to Azure Service Bus via MassTransit
- Ordering's BasketCheckoutEventHandler consumes event, converts to CreateOrderCommand
- Ensures loose coupling between services

### gRPC Communication
- Discount.Grpc exposes DiscountProtoService (defined in discount.proto)
- Basket.API has gRPC client configured with certificate validation bypass for development
- Used for synchronous discount calculation during basket operations

### Caching Strategy (Basket)
- CachedBasketRepository decorates IBasketRepository using Scrutor.Decorate<T>()
- Cache-aside pattern: check Redis first, fallback to Marten (PostgreSQL)
- Redis stores serialized shopping carts by username

### Database per Service Pattern
- **Catalog**: Marten on PostgreSQL (basketdb)
- **Basket**: Marten on PostgreSQL + Redis cache
- **Discount**: EF Core on SQLite
- **Ordering**: EF Core on SQL Server (orderdb)

## Ports and URLs (Docker)

- **Shopping.Web**: https://localhost:6065
- **Catalog.API**: http://localhost:6000
- **Basket.API**: http://localhost:6001
- **Discount.Grpc**: http://localhost:6002
- **Ordering.API**: http://localhost:6003
- **YarpApiGateway**: http://localhost:6004

## Configuration

### appsettings.json Structure
Services use configuration sections:
- **Database:ConnectionString** - Database connection strings
- **GrpcSettings:DiscountUrl** - gRPC service endpoints (Basket → Discount)
- **MessageBroker:ConnectionString** - Azure Service Bus connection string
- **ReverseProxy** - Yarp routing configuration

### Environment Variables (docker-compose.override.yml)
Override settings per service using environment variables:
- ConnectionStrings__Database
- GrpcSettings__DiscountUrl
- MessageBroker__ConnectionString

## Common Development Workflows

### Adding a New Feature to Catalog (Vertical Slice)
1. Create feature folder: `Products/NewFeature/`
2. Add Command/Query record with FluentValidation validator
3. Add Handler implementing ICommandHandler or IQueryHandler
4. Add Endpoint implementing ICarterModule
5. No registration needed - Carter auto-discovers ICarterModule

### Adding a New Feature to Ordering (DDD)
1. **Domain**: Add/modify Aggregate Root, Entities, Value Objects in Ordering.Domain
2. **Application**: Create Command/Query in Ordering.Application with handler
3. **Infrastructure**: Add EF Core configuration if new entities
4. **API**: Create Carter endpoint in Ordering.API

### Adding a New Integration Event
1. Create event class in BuildingBlocks.Messaging inheriting IntegrationEvent
2. **Publisher**: Inject IPublishEndpoint, call PublishAsync(event)
3. **Consumer**: Create handler implementing IConsumer<TEvent>
4. Register consumer in DI: builder.Services.AddConsumer<Handler>()

### Modifying gRPC Contract
1. Update discount.proto in Discount.Grpc
2. Build project to regenerate C# classes
3. Update DiscountProtoService implementation
4. Update Basket.API gRPC client calls if needed

## Docker Services

Infrastructure services defined in docker-compose.yml:
- **catalogdb, basketdb**: PostgreSQL databases
- **distributedcache**: Redis
- **orderdb**: SQL Server

Note: Azure Service Bus is used for messaging and is hosted on Azure (not in Docker)

## Health Checks

All services expose `/health` endpoint:
- Catalog: Marten health check
- Basket: Marten + Redis health checks
- Discount: SQLite health check
- Ordering: SQL Server health check

## Troubleshooting

### Services not starting
- Ensure Docker Desktop is running
- Check Docker memory allocation (4GB minimum)
- Wait for all containers to initialize (ordering service may take extra time)

### Database connection issues
- Verify connection strings in appsettings.json or docker-compose.override.yml
- Check database containers are running: `docker ps`

### Azure Service Bus not receiving events
- Verify Azure Service Bus namespace is created and running
- Check MessageBroker:ConnectionString configuration in both publisher and consumer
- Ensure connection string has correct permissions (Send for Basket, Listen for Ordering)
- View Azure Service Bus in Azure Portal to monitor queues/topics

### gRPC communication failing
- Ensure Discount.Grpc is running before Basket.API
- Check GrpcSettings:DiscountUrl configuration in Basket.API
- For local development, certificate validation is bypassed

## Reference Documentation

Medium article with detailed architecture explanation:
https://medium.com/@mehmetozkaya/net-8-microservices-ddd-cqrs-vertical-clean-architecture-2dd7ebaaf4bd
