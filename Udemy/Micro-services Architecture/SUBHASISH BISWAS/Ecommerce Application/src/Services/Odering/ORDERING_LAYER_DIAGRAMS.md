# Ordering Service - Layer Diagrams

This document contains architectural layer diagrams for the Ordering microservice.

## 1. Clean Architecture - Layer Dependencies

```mermaid
graph TB
    subgraph "Ordering Service - Clean Architecture"
        subgraph API["API Layer (Ordering.API)"]
            Controllers["Controllers<br/>- OrderController"]
            Consumers["Event Consumers<br/>- BasketCheckoutConsumer"]
            Extensions["Extensions<br/>- HostExtension"]
            APIMapping["Mapping<br/>- OrderingProfile"]
            Startup["Startup & Configuration"]
        end

        subgraph Application["Application Layer (Ordering.Application)"]
            Commands["Commands<br/>- CheckoutOrderCommand<br/>- UpdateOrderCommand<br/>- DeleteOrderCommand"]
            Queries["Queries<br/>- GetOrdersListQuery"]
            Handlers["Handlers<br/>- Command/Query Handlers"]
            Behaviors["Pipeline Behaviors<br/>- ValidationBehaviour<br/>- UnhandledExceptionBehaviour"]
            Contracts["Contracts (Interfaces)<br/>- IAsyncRepository<br/>- IOrderRepository<br/>- IEmailService"]
            DTOs["Models & DTOs<br/>- OrdersVm<br/>- Email"]
            Validators["Validators<br/>- FluentValidation"]
            AppMapping["Mapping<br/>- MappingProfile"]
        end

        subgraph Domain["Domain Layer (Ordering.Domain)"]
            Entities["Entities<br/>- Order (Simple)"]
            Aggregates["Aggregates (DDD)<br/>- Order (Rich)<br/>- OrderES (Event Sourced)"]
            ValueObjects["Value Objects<br/>- Address<br/>- Payment<br/>- OrderId<br/>- CustomerId"]
            DomainEvents["Domain Events<br/>- OrderCreatedEvent<br/>- OrderUpdatedEvent<br/>- OrderCreatedEventES"]
            Common["Common (Base Classes)<br/>- EntityBase<br/>- ValueObject<br/>- EventSourcedAggregate"]
        end

        subgraph Infrastructure["Infrastructure Layer (Ordering.Infrastructure)"]
            DbContext["Persistence<br/>- OrderContext<br/>- OrderContextSeed"]
            Repositories["Repositories<br/>- RepositoryBase<br/>- OrderRepository"]
            ExternalServices["External Services<br/>- EmailService (SendGrid)"]
            EventStore["Event Store<br/>- CosmosDbEventStore<br/>- EventSourcedRepository"]
            Migrations["EF Migrations"]
        end

        subgraph External["External Dependencies"]
            SQLServer[("SQL Server<br/>OrderDb")]
            RabbitMQ[("RabbitMQ<br/>Message Broker")]
            SendGrid[("SendGrid<br/>Email Service")]
            CosmosDB[("Azure CosmosDB<br/>Event Store")]
        end
    end

    %% Dependencies (outer layers depend on inner layers)
    API --> Application
    API --> Infrastructure
    Application --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain

    %% External dependencies
    Infrastructure --> SQLServer
    Infrastructure --> CosmosDB
    Infrastructure --> SendGrid
    API --> RabbitMQ

    %% Styling
    classDef apiLayer fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef appLayer fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef domainLayer fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef infraLayer fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    classDef externalLayer fill:#fce4ec,stroke:#880e4f,stroke-width:2px

    class API apiLayer
    class Application appLayer
    class Domain domainLayer
    class Infrastructure infraLayer
    class External externalLayer
```

## 2. CQRS Pattern - Command and Query Separation

```mermaid
graph LR
    subgraph Client["External Clients"]
        WebApp["Web Application"]
        APIGateway["API Gateway"]
        OtherServices["Other Microservices"]
    end

    subgraph OrderingAPI["Ordering.API"]
        Controller["OrderController"]
        Consumer["BasketCheckoutConsumer"]
    end

    subgraph MediatR["MediatR Pipeline"]
        Mediator["Mediator"]
        ValidationPipeline["Validation Pipeline"]
        ExceptionPipeline["Exception Pipeline"]
    end

    subgraph Commands["Write Side (Commands)"]
        CheckoutCmd["CheckoutOrderCommand"]
        UpdateCmd["UpdateOrderCommand"]
        DeleteCmd["DeleteOrderCommand"]

        CheckoutHandler["CheckoutOrderCommandHandler"]
        UpdateHandler["UpdateOrderCommandHandler"]
        DeleteHandler["DeleteOrderCommandHandler"]

        WriteRepo["IOrderRepository (Write)"]
    end

    subgraph Queries["Read Side (Queries)"]
        GetOrdersQuery["GetOrdersListQuery"]

        GetOrdersHandler["GetOrderListQueryHandler"]

        ReadRepo["IOrderRepository (Read)"]
    end

    subgraph Database["Data Store"]
        WriteDB[("SQL Server<br/>Write Operations<br/>Tracking Enabled")]
        ReadDB[("SQL Server<br/>Read Operations<br/>AsNoTracking")]
    end

    %% Client flows
    WebApp -->|POST/PUT/DELETE| Controller
    APIGateway -->|HTTP Requests| Controller
    OtherServices -->|Events via RabbitMQ| Consumer

    WebApp -->|GET| Controller

    %% Command flow
    Controller -->|Send Command| Mediator
    Consumer -->|Send Command| Mediator
    Mediator -->|Write Operation| ValidationPipeline
    ValidationPipeline -->|Validate| ExceptionPipeline

    ExceptionPipeline --> CheckoutCmd
    ExceptionPipeline --> UpdateCmd
    ExceptionPipeline --> DeleteCmd

    CheckoutCmd --> CheckoutHandler
    UpdateCmd --> UpdateHandler
    DeleteCmd --> DeleteHandler

    CheckoutHandler --> WriteRepo
    UpdateHandler --> WriteRepo
    DeleteHandler --> WriteRepo

    WriteRepo -->|INSERT/UPDATE/DELETE| WriteDB

    %% Query flow
    Controller -->|Send Query| Mediator
    Mediator -->|Read Operation| GetOrdersQuery
    GetOrdersQuery --> GetOrdersHandler
    GetOrdersHandler --> ReadRepo
    ReadRepo -->|SELECT| ReadDB

    %% Styling
    classDef clientStyle fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    classDef writeStyle fill:#ffebee,stroke:#c62828,stroke-width:2px
    classDef readStyle fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef mediatorStyle fill:#fff3e0,stroke:#ef6c00,stroke-width:2px
    classDef dbStyle fill:#f3e5f5,stroke:#6a1b9a,stroke-width:3px

    class WebApp,APIGateway,OtherServices clientStyle
    class CheckoutCmd,UpdateCmd,DeleteCmd,CheckoutHandler,UpdateHandler,DeleteHandler,WriteRepo,WriteDB writeStyle
    class GetOrdersQuery,GetOrdersHandler,ReadRepo,ReadDB readStyle
    class Mediator,ValidationPipeline,ExceptionPipeline mediatorStyle
```

## 3. Event-Driven Architecture - Integration Events

```mermaid
graph TB
    subgraph BasketService["Basket Microservice"]
        BasketAPI["Basket.API"]
        BasketController["BasketController"]
    end

    subgraph MessageBus["Message Bus Infrastructure"]
        MassTransit["MassTransit"]
        RabbitMQ[("RabbitMQ<br/>Message Broker")]
        Queue["BasketCheckoutQueue"]
    end

    subgraph OrderingService["Ordering Microservice"]
        Consumer["BasketCheckoutConsumer<br/>(MassTransit Consumer)"]
        MediatR["MediatR"]
        CommandHandler["CheckoutOrderCommandHandler"]
        Repository["OrderRepository"]
        EmailService["EmailService"]
    end

    subgraph Events["Integration Events"]
        BasketCheckoutEvent["BasketCheckoutEvent<br/>---<br/>UserName<br/>TotalPrice<br/>FirstName, LastName<br/>Email, Address<br/>PaymentMethod<br/>CardNumber, etc."]
    end

    subgraph DataStores["Data Stores"]
        OrderDB[("SQL Server<br/>OrderDb")]
        SendGrid[("SendGrid<br/>Email Service")]
    end

    %% Flow
    BasketController -->|1. Checkout Request| BasketAPI
    BasketAPI -->|2. Publish Event| MassTransit
    MassTransit -->|3. Send to Queue| RabbitMQ
    RabbitMQ -->|4. Store in| Queue

    Queue -->|5. Consume| Consumer
    Consumer -->|6. Map to Command| MediatR
    MediatR -->|7. Send Command| CommandHandler

    CommandHandler -->|8. Create Order| Repository
    Repository -->|9. INSERT| OrderDB

    CommandHandler -->|10. Send Email| EmailService
    EmailService -->|11. Send via API| SendGrid

    BasketCheckoutEvent -.->|Event Schema| Queue

    %% Styling
    classDef basketStyle fill:#e1f5ff,stroke:#01579b,stroke-width:2px
    classDef messagingStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef orderingStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef eventStyle fill:#e8f5e9,stroke:#1b5e20,stroke-width:2px
    classDef dataStyle fill:#fce4ec,stroke:#880e4f,stroke-width:3px

    class BasketAPI,BasketController basketStyle
    class MassTransit,RabbitMQ,Queue messagingStyle
    class Consumer,MediatR,CommandHandler,Repository,EmailService orderingStyle
    class BasketCheckoutEvent eventStyle
    class OrderDB,SendGrid dataStyle
```

## 4. Domain-Driven Design - Domain Layer Structure

```mermaid
graph TB
    subgraph DomainLayer["Ordering.Domain Layer"]
        subgraph Common["Common (Base Classes)"]
            EntityBase["EntityBase<br/>---<br/>+ Id: long<br/>+ CreatedBy: string<br/>+ CreatedDate: DateTime<br/>+ LastModifiedBy: string<br/>+ LastModifiedDate: DateTime"]

            ValueObjectBase["ValueObject (Abstract)<br/>---<br/>+ GetEqualityComponents()<br/>+ Equals()<br/>+ GetHashCode()"]

            EventSourcedBase["EventSourcedAggregate<br/>---<br/>+ Id: Guid<br/>+ Version: int<br/>+ GetUncommittedEvents()<br/>+ MarkEventsAsCommitted()<br/>+ LoadFromHistory()<br/>+ ApplyEvent()"]
        end

        subgraph Entities["Entities (Simple CRUD)"]
            Order["Order<br/>---<br/>+ UserName: string<br/>+ TotalPrice: decimal<br/>+ FirstName: string<br/>+ LastName: string<br/>+ EmailAddress: string<br/>+ AddressLine: string<br/>+ PaymentMethod: int<br/>+ CardNumber: string"]
        end

        subgraph Aggregates["Aggregates (DDD)"]
            OrderAggregate["Order (Rich Domain Model)<br/>---<br/>+ OrderId: OrderId<br/>+ CustomerId: CustomerId<br/>+ OrderName: OrderName<br/>+ ShippingAddress: Address<br/>+ BillingAddress: Address<br/>+ Payment: Payment<br/>+ Status: OrderStatus<br/>+ OrderItems: List<br/>---<br/>+ Create(): Order<br/>+ Update()<br/>+ Add(OrderItem)<br/>+ Remove(OrderItem)"]

            OrderESAggregate["OrderES (Event Sourced)<br/>---<br/>+ OrderId: Guid<br/>+ CustomerId: Guid<br/>+ OrderName: string<br/>+ ShippingAddress: Address<br/>+ Payment: Payment<br/>+ Status: string<br/>---<br/>+ Create(): OrderES<br/>+ Update()<br/>+ AddItem()<br/>+ RemoveItem()<br/>- Apply(OrderCreatedEventES)<br/>- Apply(OrderUpdatedEventES)<br/>- Apply(OrderItemAddedEvent)"]
        end

        subgraph ValueObjects["Value Objects"]
            Address["Address<br/>---<br/>+ FirstName: string<br/>+ LastName: string<br/>+ EmailAddress: string<br/>+ AddressLine: string<br/>+ Country: string<br/>+ State: string<br/>+ ZipCode: string<br/>---<br/>+ Of(): Address"]

            Payment["Payment<br/>---<br/>+ CardName: string<br/>+ CardNumber: string<br/>+ Expiration: string<br/>+ CVV: string<br/>+ PaymentMethod: int<br/>---<br/>+ Of(): Payment"]

            StronglyTypedIds["Strongly-Typed IDs<br/>---<br/>OrderId<br/>CustomerId<br/>ProductId<br/>---<br/>+ Of(Guid): TypedId<br/>+ Value: Guid"]

            OtherVOs["Other Value Objects<br/>---<br/>OrderName<br/>OrderStatus"]
        end

        subgraph Events["Domain Events"]
            TraditionalEvents["Traditional Events<br/>---<br/>OrderCreatedEvent<br/>OrderUpdatedEvent"]

            ESEvents["Event Sourcing Events<br/>---<br/>OrderCreatedEventES<br/>OrderUpdatedEventES<br/>OrderItemAddedEvent<br/>OrderItemRemovedEvent"]
        end
    end

    %% Inheritance relationships
    EntityBase -.->|inherits| Order
    EntityBase -.->|inherits| OrderAggregate
    EventSourcedBase -.->|inherits| OrderESAggregate
    ValueObjectBase -.->|inherits| Address
    ValueObjectBase -.->|inherits| Payment
    ValueObjectBase -.->|inherits| StronglyTypedIds
    ValueObjectBase -.->|inherits| OtherVOs

    %% Composition relationships
    OrderAggregate -->|contains| Address
    OrderAggregate -->|contains| Payment
    OrderAggregate -->|contains| StronglyTypedIds
    OrderAggregate -->|raises| TraditionalEvents

    OrderESAggregate -->|contains| Address
    OrderESAggregate -->|contains| Payment
    OrderESAggregate -->|raises & applies| ESEvents

    %% Styling
    classDef baseStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef entityStyle fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    classDef aggregateStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef voStyle fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef eventStyle fill:#ffebee,stroke:#c62828,stroke-width:2px

    class EntityBase,ValueObjectBase,EventSourcedBase baseStyle
    class Order entityStyle
    class OrderAggregate,OrderESAggregate aggregateStyle
    class Address,Payment,StronglyTypedIds,OtherVOs voStyle
    class TraditionalEvents,ESEvents eventStyle
```

## 5. Event Sourcing Architecture

```mermaid
graph TB
    subgraph CommandSide["Command Side (Write)"]
        Command["CreateOrder /<br/>UpdateOrder Command"]
        CommandHandler["Command Handler (ES)"]
        AggregateWrite["OrderES Aggregate<br/>(In-Memory State)"]
    end

    subgraph DomainLogic["Domain Logic"]
        BusinessMethod["Business Methods<br/>Create() / Update() /<br/>AddItem() / RemoveItem()"]
        CreateEvent["Create Domain Event<br/>(OrderCreatedEventES, etc.)"]
        ApplyEvent["ApplyEvent(event)<br/>(Reflection-based routing)"]
        ApplyMethod["Apply(EventType)<br/>(State mutation)"]
        AddToUncommitted["Add to<br/>Uncommitted Events"]
    end

    subgraph EventStore["Event Store"]
        Repository["EventSourcedRepository<T>"]
        Store["CosmosDbEventStore<br/>(IEventStore)"]
        OptimisticLock["Optimistic Concurrency<br/>Version Check"]
        Serialize["Serialize Events<br/>to JSON"]
    end

    subgraph Storage["Event Storage"]
        CosmosDB[("Azure CosmosDB<br/>Event Stream<br/>---<br/>AggregateId (PK)<br/>EventType<br/>EventData (JSON)<br/>Version<br/>Timestamp")]
    end

    subgraph QuerySide["Query Side (Read)"]
        QueryCommand["Get Order Query"]
        QueryHandler["Query Handler"]
        LoadAggregate["Load Aggregate<br/>from Event Store"]
        GetEvents["Get All Events<br/>for AggregateId"]
        Deserialize["Deserialize Events<br/>from JSON"]
        Replay["Replay Events<br/>LoadFromHistory()"]
        RebuiltAggregate["OrderES Aggregate<br/>(Rebuilt State)"]
    end

    %% Write flow
    Command --> CommandHandler
    CommandHandler --> AggregateWrite
    AggregateWrite --> BusinessMethod
    BusinessMethod --> CreateEvent
    CreateEvent --> ApplyEvent
    ApplyEvent --> ApplyMethod
    ApplyMethod --> AddToUncommitted

    AddToUncommitted --> Repository
    Repository --> Store
    Store --> OptimisticLock
    OptimisticLock -->|No Conflict| Serialize
    Serialize --> CosmosDB

    OptimisticLock -.->|Conflict| CommandHandler

    %% Read flow
    QueryCommand --> QueryHandler
    QueryHandler --> LoadAggregate
    LoadAggregate --> Repository
    Repository --> GetEvents
    GetEvents --> CosmosDB
    CosmosDB --> Deserialize
    Deserialize --> Replay
    Replay --> RebuiltAggregate
    RebuiltAggregate --> QueryHandler

    %% Event loop visualization
    subgraph EventReplay["Event Replay Process"]
        Event1["OrderCreatedEventES<br/>↓<br/>Apply() → Set initial state"]
        Event2["OrderItemAddedEvent<br/>↓<br/>Apply() → Add to Items"]
        Event3["OrderUpdatedEventES<br/>↓<br/>Apply() → Update fields"]

        Event1 --> Event2 --> Event3
    end

    Replay -.-> EventReplay

    %% Styling
    classDef writeStyle fill:#ffebee,stroke:#c62828,stroke-width:2px
    classDef domainStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef storeStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef dbStyle fill:#e1f5ff,stroke:#01579b,stroke-width:3px
    classDef readStyle fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef replayStyle fill:#fce4ec,stroke:#880e4f,stroke-width:2px

    class Command,CommandHandler,AggregateWrite writeStyle
    class BusinessMethod,CreateEvent,ApplyEvent,ApplyMethod,AddToUncommitted domainStyle
    class Repository,Store,OptimisticLock,Serialize storeStyle
    class CosmosDB dbStyle
    class QueryCommand,QueryHandler,LoadAggregate,GetEvents,Deserialize,Replay,RebuiltAggregate readStyle
    class Event1,Event2,Event3 replayStyle
```

## 6. Infrastructure Layer - Data Access

```mermaid
graph TB
    subgraph ApplicationLayer["Application Layer"]
        CommandHandlers["Command Handlers"]
        QueryHandlers["Query Handlers"]

        IAsyncRepo["IAsyncRepository&lt;T&gt;<br/>(Interface)"]
        IOrderRepo["IOrderRepository<br/>(Interface)"]
        IEmailSvc["IEmailService<br/>(Interface)"]
    end

    subgraph InfrastructureLayer["Infrastructure Layer"]
        subgraph Repositories["Repository Implementations"]
            RepoBase["RepositoryBase&lt;T&gt;<br/>---<br/>+ GetAllAsync()<br/>+ GetAsync(predicate, orderBy, includes)<br/>+ GetByIdAsync()<br/>+ AddAsync() / UpdateAsync()<br/>+ DeleteAsync()"]

            OrderRepo["OrderRepository<br/>---<br/>+ GetOrderByUserName(userName)"]
        end

        subgraph Context["Database Context"]
            OrderContext["OrderContext : DbContext<br/>---<br/>+ DbSet&lt;Order&gt; Orders<br/>+ SaveChangesAsync()<br/>  (Auto-set audit fields)"]

            ContextSeed["OrderContextSeed<br/>---<br/>+ SeedAsync(context, logger)<br/>  (Seed initial data)"]
        end

        subgraph Services["External Services"]
            EmailService["EmailService<br/>---<br/>+ SendEmail(Email)<br/>  (SendGrid integration)"]
        end

        subgraph EventStoreInfra["Event Store (ES)"]
            CosmosEventStore["CosmosDbEventStore<br/>---<br/>+ SaveEventsAsync()<br/>+ GetEventsAsync()<br/>+ GetVersionAsync()"]

            ESRepo["EventSourcedRepository&lt;T&gt;<br/>---<br/>+ GetByIdAsync()<br/>+ SaveAsync()"]
        end

        subgraph Migrations["EF Core Migrations"]
            InitialMigration["20220531074045_InitialCreate<br/>Creates Orders table"]
        end
    end

    subgraph ExternalSystems["External Systems"]
        SQLServer[("SQL Server<br/>OrderDb<br/>---<br/>Port: 1433<br/>Database: OrderDb")]

        CosmosDB[("Azure CosmosDB<br/>Event Store<br/>---<br/>Partition: AggregateId<br/>Sort: Version")]

        SendGridAPI[("SendGrid API<br/>Email Delivery")]
    end

    %% Interface implementations
    IAsyncRepo -.->|implements| RepoBase
    IOrderRepo -.->|implements| OrderRepo
    IEmailSvc -.->|implements| EmailService

    %% Inheritance
    RepoBase -.->|inherits| OrderRepo

    %% Dependencies
    CommandHandlers --> IOrderRepo
    CommandHandlers --> IEmailSvc
    QueryHandlers --> IOrderRepo

    RepoBase --> OrderContext
    OrderRepo --> OrderContext

    OrderContext --> SQLServer
    ContextSeed --> OrderContext
    InitialMigration -.->|creates schema| SQLServer

    EmailService --> SendGridAPI

    CosmosEventStore --> CosmosDB
    ESRepo --> CosmosEventStore

    %% Styling
    classDef interfaceStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef repoStyle fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    classDef contextStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef serviceStyle fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef esStyle fill:#ffebee,stroke:#c62828,stroke-width:2px
    classDef dbStyle fill:#fce4ec,stroke:#880e4f,stroke-width:3px

    class IAsyncRepo,IOrderRepo,IEmailSvc interfaceStyle
    class RepoBase,OrderRepo repoStyle
    class OrderContext,ContextSeed,InitialMigration contextStyle
    class EmailService serviceStyle
    class CosmosEventStore,ESRepo esStyle
    class SQLServer,CosmosDB,SendGridAPI dbStyle
```

## 7. MediatR Pipeline - Cross-Cutting Concerns

```mermaid
graph LR
    subgraph Request["Incoming Request"]
        TRequest["TRequest<br/>(Command or Query)"]
    end

    subgraph Pipeline["MediatR Pipeline Behaviors"]
        Behavior1["UnhandledExceptionBehaviour<br/>---<br/>1. Wrap in try-catch<br/>2. Log exceptions<br/>3. Re-throw"]

        Behavior2["ValidationBehaviour<br/>---<br/>1. Get all IValidator&lt;TRequest&gt;<br/>2. Run validations<br/>3. Collect failures<br/>4. Throw if any failures"]
    end

    subgraph Handler["Request Handler"]
        ActualHandler["IRequestHandler&lt;TRequest, TResponse&gt;<br/>---<br/>Business logic execution"]
    end

    subgraph Response["Response"]
        TResponse["TResponse<br/>(Result)"]
        ValidationEx["ValidationException<br/>(400 Bad Request)"]
        Exception["Exception<br/>(500 Internal Server Error)"]
    end

    TRequest --> Behavior1
    Behavior1 -->|await next()| Behavior2
    Behavior2 -->|Validation passed| ActualHandler
    Behavior2 -.->|Validation failed| ValidationEx

    ActualHandler -->|Success| TResponse
    ActualHandler -.->|Unhandled error| Behavior1
    Behavior1 -.->|Logged & re-thrown| Exception

    %% Styling
    classDef requestStyle fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    classDef pipelineStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef handlerStyle fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef successStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef errorStyle fill:#ffebee,stroke:#c62828,stroke-width:2px

    class TRequest requestStyle
    class Behavior1,Behavior2 pipelineStyle
    class ActualHandler handlerStyle
    class TResponse successStyle
    class ValidationEx,Exception errorStyle
```

## 8. Complete System Architecture - Microservices Context

```mermaid
graph TB
    subgraph Clients["External Clients"]
        Browser["Web Browser"]
        Mobile["Mobile App"]
        External["External Systems"]
    end

    subgraph APIGateway["API Gateway Layer"]
        Ocelot["Ocelot API Gateway<br/>Port: 8010"]
        Aggregator["Shopping Aggregator<br/>Port: 8005"]
    end

    subgraph Microservices["Microservices"]
        Catalog["Catalog.API<br/>MongoDB<br/>Port: 8000"]
        Basket["Basket.API<br/>Redis<br/>Port: 8001"]
        Discount["Discount.API<br/>PostgreSQL<br/>Port: 8002"]
        DiscountGrpc["Discount.gRPC<br/>PostgreSQL<br/>Port: 8003"]

        Ordering["Ordering.API<br/>SQL Server<br/>Port: 8004"]
    end

    subgraph MessageBroker["Message Broker"]
        RabbitMQ["RabbitMQ<br/>Ports: 5672, 15672"]
    end

    subgraph OrderingDetails["Ordering Service Internal"]
        OrderAPI["Ordering.API"]
        OrderApp["Ordering.Application<br/>(CQRS + MediatR)"]
        OrderDomain["Ordering.Domain<br/>(DDD)"]
        OrderInfra["Ordering.Infrastructure"]

        OrderAPI --> OrderApp
        OrderApp --> OrderDomain
        OrderInfra --> OrderApp
    end

    subgraph Databases["Data Stores"]
        MongoDB[("MongoDB<br/>Catalog")]
        Redis[("Redis<br/>Basket Cache")]
        PostgreSQL[("PostgreSQL<br/>Discount")]
        SQLServer[("SQL Server<br/>Orders")]
        CosmosDB[("CosmosDB<br/>Event Store")]
    end

    subgraph ExternalServices["External Services"]
        SendGrid[("SendGrid<br/>Email")]
    end

    subgraph Management["Management Tools"]
        Portainer["Portainer<br/>Port: 9000"]
        PgAdmin["pgAdmin<br/>Port: 5050"]
        RabbitMgmt["RabbitMQ Management<br/>Port: 15672"]
    end

    %% Client flows
    Browser --> Ocelot
    Mobile --> Ocelot
    External --> Aggregator

    %% Gateway routing
    Ocelot --> Catalog
    Ocelot --> Basket
    Ocelot --> Discount
    Ocelot --> Ordering

    Aggregator --> Catalog
    Aggregator --> Basket
    Aggregator --> Ordering

    %% Inter-service communication
    Basket -->|gRPC| DiscountGrpc
    Basket -->|Publish Event| RabbitMQ
    RabbitMQ -->|Consume Event| Ordering

    %% Ordering internal
    Ordering --> OrderingDetails

    %% Database connections
    Catalog --> MongoDB
    Basket --> Redis
    Discount --> PostgreSQL
    DiscountGrpc --> PostgreSQL
    OrderInfra --> SQLServer
    OrderInfra --> CosmosDB
    OrderInfra --> SendGrid

    %% Management connections
    Portainer -.->|Manage| Microservices
    PgAdmin -.->|Manage| PostgreSQL
    RabbitMgmt -.->|Manage| RabbitMQ

    %% Styling
    classDef clientStyle fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    classDef gatewayStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px
    classDef serviceStyle fill:#f3e5f5,stroke:#4a148c,stroke-width:2px
    classDef orderingStyle fill:#ffebee,stroke:#c62828,stroke-width:3px
    classDef messagingStyle fill:#ffe0b2,stroke:#e65100,stroke-width:2px
    classDef dbStyle fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px
    classDef externalStyle fill:#fce4ec,stroke:#880e4f,stroke-width:2px
    classDef mgmtStyle fill:#f3e5f5,stroke:#6a1b9a,stroke-width:2px

    class Browser,Mobile,External clientStyle
    class Ocelot,Aggregator gatewayStyle
    class Catalog,Basket,Discount,DiscountGrpc serviceStyle
    class Ordering,OrderAPI,OrderApp,OrderDomain,OrderInfra orderingStyle
    class RabbitMQ messagingStyle
    class MongoDB,Redis,PostgreSQL,SQLServer,CosmosDB dbStyle
    class SendGrid externalStyle
    class Portainer,PgAdmin,RabbitMgmt mgmtStyle
```

## 9. Data Flow - Traditional CRUD vs Event Sourcing

```mermaid
graph TB
    subgraph Traditional["Traditional CRUD Approach"]
        TraditionalAPI["API Request"]
        TraditionalCommand["Command"]
        TraditionalHandler["Command Handler"]
        TraditionalRepo["Repository"]
        TraditionalEF["Entity Framework"]
        TraditionalDB[("SQL Server<br/>Current State Only<br/>---<br/>Orders Table<br/>Latest values")]

        TraditionalAPI --> TraditionalCommand
        TraditionalCommand --> TraditionalHandler
        TraditionalHandler --> TraditionalRepo
        TraditionalRepo --> TraditionalEF
        TraditionalEF -->|UPDATE/INSERT| TraditionalDB

        Note1["❌ Lost History<br/>❌ No Audit Trail<br/>❌ Can't replay events<br/>✅ Simple queries<br/>✅ Familiar pattern"]
    end

    subgraph EventSourced["Event Sourcing Approach"]
        ESApi["API Request"]
        ESCommand["Command"]
        ESHandler["Command Handler (ES)"]
        ESAggregate["OrderES Aggregate"]
        ESDomainEvent["Domain Event"]
        ESRepo["EventSourcedRepository"]
        ESStore["CosmosDbEventStore"]
        ESCosmosDB[("CosmosDB<br/>Event Stream<br/>---<br/>OrderCreatedEventES<br/>OrderUpdatedEventES<br/>OrderItemAddedEvent<br/>...all events")]

        ESProjection["Projection<br/>(Future: Read Model)"]
        ESReadDB[("Read Database<br/>Materialized Views")]

        ESApi --> ESCommand
        ESCommand --> ESHandler
        ESHandler --> ESAggregate
        ESAggregate --> ESDomainEvent
        ESDomainEvent --> ESRepo
        ESRepo --> ESStore
        ESStore -->|Append Events| ESCosmosDB

        ESCosmosDB -.->|Event replay| ESAggregate
        ESCosmosDB -.->|Project| ESProjection
        ESProjection -.->|Materialize| ESReadDB

        Note2["✅ Full Audit History<br/>✅ Time Travel<br/>✅ Event Replay<br/>✅ Temporal Queries<br/>❌ Complex queries<br/>❌ Learning curve"]
    end

    %% Styling
    classDef traditionalStyle fill:#e3f2fd,stroke:#1565c0,stroke-width:2px
    classDef esStyle fill:#ffebee,stroke:#c62828,stroke-width:2px
    classDef noteStyle fill:#fff3e0,stroke:#e65100,stroke-width:2px

    class TraditionalAPI,TraditionalCommand,TraditionalHandler,TraditionalRepo,TraditionalEF,TraditionalDB traditionalStyle
    class ESApi,ESCommand,ESHandler,ESAggregate,ESDomainEvent,ESRepo,ESStore,ESCosmosDB,ESProjection,ESReadDB esStyle
    class Note1,Note2 noteStyle
```

---

## Tools for Viewing

These Mermaid diagrams can be viewed in:
- **GitHub** (native support)
- **Visual Studio Code** (with Mermaid extension: `bierner.markdown-mermaid`)
- **Online**: https://mermaid.live/
- **Markdown Preview Enhanced** (VS Code extension)
- **Any Markdown viewer with Mermaid support**

## Diagram Descriptions

1. **Clean Architecture Layer Dependencies** - Shows the 4-layer architecture and dependency rules
2. **CQRS Pattern** - Illustrates command/query separation with MediatR
3. **Event-Driven Architecture** - Integration events flow via RabbitMQ
4. **DDD Domain Layer Structure** - Domain entities, aggregates, value objects, and inheritance
5. **Event Sourcing Architecture** - Write/read sides with event store and replay
6. **Infrastructure Layer** - Repository pattern, DbContext, and external service integrations
7. **MediatR Pipeline** - Cross-cutting concerns (validation, exception handling)
8. **Complete System Architecture** - Entire microservices ecosystem with Ordering service highlighted
9. **Traditional CRUD vs Event Sourcing** - Comparison of both approaches with trade-offs
