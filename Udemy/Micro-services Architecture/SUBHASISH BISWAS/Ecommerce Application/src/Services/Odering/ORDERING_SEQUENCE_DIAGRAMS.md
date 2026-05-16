# Ordering Service - Sequence Diagrams

This document contains sequence diagrams for key flows in the Ordering microservice.

## 1. Basket Checkout to Order Creation Flow

```mermaid
sequenceDiagram
    participant User
    participant BasketAPI as Basket.API
    participant RabbitMQ
    participant Consumer as BasketCheckoutConsumer
    participant MediatR
    participant Validator as ValidationBehaviour
    participant Handler as CheckoutOrderCommandHandler
    participant Repository as OrderRepository
    participant DB as SQL Server
    participant Email as EmailService
    participant SendGrid

    User->>BasketAPI: POST /api/v1/Basket/Checkout
    BasketAPI->>BasketAPI: Create BasketCheckoutEvent
    BasketAPI->>RabbitMQ: Publish to BasketCheckoutQueue
    BasketAPI-->>User: 202 Accepted

    RabbitMQ->>Consumer: Consume BasketCheckoutEvent
    Consumer->>Consumer: Map Event to CheckoutOrderCommand
    Consumer->>MediatR: Send(CheckoutOrderCommand)

    MediatR->>Validator: Validate command
    Validator->>Validator: Run FluentValidation rules
    alt Validation Fails
        Validator-->>Consumer: ValidationException
        Consumer-->>RabbitMQ: NACK message
    else Validation Succeeds
        Validator->>Handler: Execute handler
        Handler->>Handler: Map command to Order entity
        Handler->>Repository: AddAsync(order)
        Repository->>DB: INSERT INTO Orders
        DB-->>Repository: Order ID
        Repository-->>Handler: Order ID

        Handler->>Email: SendEmail(order details)
        Email->>SendGrid: Send via API
        SendGrid-->>Email: Success/Failure
        Email-->>Handler: Email sent status

        Handler-->>MediatR: Order ID
        MediatR-->>Consumer: Order ID
        Consumer->>Consumer: Log success
        Consumer-->>RabbitMQ: ACK message
    end
```

## 2. Create Order Command Flow (Direct API Call)

```mermaid
sequenceDiagram
    participant Client
    participant Controller as OrderController
    participant MediatR
    participant Validator as ValidationBehaviour
    participant Handler as CheckoutOrderCommandHandler
    participant Mapper as AutoMapper
    participant Repository as OrderRepository
    participant Context as OrderContext
    participant DB as SQL Server
    participant Email as EmailService

    Client->>Controller: POST /api/v1/Order
    Note over Client,Controller: Body: CheckoutOrderCommand

    Controller->>MediatR: Send(CheckoutOrderCommand)

    MediatR->>Validator: Execute pipeline behavior
    Validator->>Validator: Get validators for command
    Validator->>Validator: Run FluentValidation

    alt Validation Errors
        Validator->>Validator: Collect validation failures
        Validator-->>Controller: throw ValidationException
        Controller-->>Client: 400 Bad Request (validation errors)
    else Valid Command
        Validator->>Handler: Continue to handler

        Handler->>Mapper: Map(CheckoutOrderCommand → Order)
        Mapper-->>Handler: Order entity

        Handler->>Repository: AddAsync(order)
        Repository->>Context: Add(order)
        Repository->>Context: SaveChangesAsync()

        Context->>Context: Set CreatedDate = DateTime.UtcNow
        Context->>Context: Set CreatedBy = "subhasish"
        Context->>DB: INSERT INTO Orders VALUES (...)
        DB-->>Context: New Order ID
        Context-->>Repository: Order with ID
        Repository-->>Handler: Order ID

        Handler->>Email: SendEmail(Email object)
        Note over Handler,Email: To, Subject, Body
        Email->>Email: Send via SendGrid API
        Email-->>Handler: bool success

        Handler->>Handler: Log email status
        Handler-->>MediatR: int orderId
        MediatR-->>Controller: int orderId
        Controller-->>Client: 200 OK (orderId)
    end
```

## 3. Get Orders by Username Query Flow

```mermaid
sequenceDiagram
    participant Client
    participant Controller as OrderController
    participant MediatR
    participant Handler as GetOrderListQueryHandler
    participant Repository as OrderRepository
    participant DB as SQL Server
    participant Mapper as AutoMapper

    Client->>Controller: GET /api/v1/Order/{userName}

    Controller->>Controller: Create GetOrdersListQuery
    Controller->>MediatR: Send(GetOrdersListQuery)

    MediatR->>Handler: Handle query

    Handler->>Repository: GetOrderByUserName(userName)
    Repository->>Repository: Build LINQ query
    Note over Repository: Where(o => o.UserName == userName)
    Repository->>DB: SELECT * FROM Orders WHERE UserName = @p0
    DB-->>Repository: List<Order> entities
    Repository-->>Handler: List<Order>

    Handler->>Mapper: Map(List<Order> → List<OrdersVm>)
    Mapper-->>Handler: List<OrdersVm>

    Handler-->>MediatR: List<OrdersVm>
    MediatR-->>Controller: List<OrdersVm>
    Controller-->>Client: 200 OK (List<OrdersVm>)
```

## 4. Update Order Command Flow

```mermaid
sequenceDiagram
    participant Client
    participant Controller as OrderController
    participant MediatR
    participant Validator as ValidationBehaviour
    participant Handler as UpdateOrderCommandHandler
    participant Mapper as AutoMapper
    participant Repository as OrderRepository
    participant Context as OrderContext
    participant DB as SQL Server

    Client->>Controller: PUT /api/v1/Order
    Note over Client,Controller: Body: UpdateOrderCommand

    Controller->>MediatR: Send(UpdateOrderCommand)

    MediatR->>Validator: Execute pipeline behavior
    Validator->>Validator: Run UpdateOrderCommandValidator

    alt Validation Fails
        Validator-->>Controller: ValidationException
        Controller-->>Client: 400 Bad Request
    else Valid Command
        Validator->>Handler: Continue to handler

        Handler->>Repository: GetByIdAsync(command.Id)
        Repository->>DB: SELECT * FROM Orders WHERE Id = @p0

        alt Order Not Found
            DB-->>Repository: null
            Repository-->>Handler: null
            Handler-->>Controller: NotFoundException
            Controller-->>Client: 404 Not Found
        else Order Found
            DB-->>Repository: Order entity
            Repository-->>Handler: Order entity

            Handler->>Mapper: Map(UpdateOrderCommand → Order)
            Note over Handler,Mapper: Updates entity properties
            Mapper-->>Handler: Updated Order

            Handler->>Repository: UpdateAsync(order)
            Repository->>Context: Update(order)
            Repository->>Context: SaveChangesAsync()

            Context->>Context: Set LastModifiedDate = DateTime.UtcNow
            Context->>Context: Set LastModifiedBy = "subhasish"
            Context->>DB: UPDATE Orders SET ... WHERE Id = @p0
            DB-->>Context: Rows affected
            Context-->>Repository: Success
            Repository-->>Handler: Success

            Handler-->>MediatR: Unit (void)
            MediatR-->>Controller: Unit
            Controller-->>Client: 204 No Content
        end
    end
```

## 5. Delete Order Command Flow

```mermaid
sequenceDiagram
    participant Client
    participant Controller as OrderController
    participant MediatR
    participant Handler as DeleteOrderCommandHandler
    participant Repository as OrderRepository
    participant Context as OrderContext
    participant DB as SQL Server

    Client->>Controller: DELETE /api/v1/Order/{id}

    Controller->>Controller: Create DeleteOrderCommand(id)
    Controller->>MediatR: Send(DeleteOrderCommand)

    MediatR->>Handler: Handle command

    Handler->>Repository: GetByIdAsync(command.Id)
    Repository->>DB: SELECT * FROM Orders WHERE Id = @p0

    alt Order Not Found
        DB-->>Repository: null
        Repository-->>Handler: null
        Handler-->>Controller: NotFoundException
        Controller-->>Client: 404 Not Found
    else Order Found
        DB-->>Repository: Order entity
        Repository-->>Handler: Order entity

        Handler->>Repository: DeleteAsync(order)
        Repository->>Context: Remove(order)
        Repository->>Context: SaveChangesAsync()
        Context->>DB: DELETE FROM Orders WHERE Id = @p0
        DB-->>Context: Rows affected
        Context-->>Repository: Success
        Repository-->>Handler: Success

        Handler-->>MediatR: Unit (void)
        MediatR-->>Controller: Unit
        Controller-->>Client: 204 No Content
    end
```

## 6. Database Migration on Application Startup

```mermaid
sequenceDiagram
    participant Program
    participant HostExtension as MigrateDatabase Extension
    participant ServiceProvider
    participant Context as OrderContext
    participant DB as SQL Server
    participant Seeder as OrderContextSeed
    participant Logger

    Program->>Program: CreateHostBuilder(args).Build()
    Program->>HostExtension: host.MigrateDatabase<OrderContext>(seeder)

    HostExtension->>HostExtension: Create service scope
    HostExtension->>ServiceProvider: GetService<OrderContext>()
    ServiceProvider-->>HostExtension: OrderContext instance

    HostExtension->>Logger: Log "Migrating database..."

    loop Retry up to 50 times
        alt Migration Succeeds
            HostExtension->>Context: Database.Migrate()
            Context->>DB: Apply pending migrations
            DB-->>Context: Success
            Context-->>HostExtension: Success

            HostExtension->>Seeder: SeedAsync(context, logger)
            Seeder->>Context: Orders.Any()?

            alt Database Empty
                Context-->>Seeder: false
                Seeder->>Seeder: Create seed orders
                Seeder->>Context: AddRange(seedOrders)
                Seeder->>Context: SaveChangesAsync()
                Context->>DB: INSERT INTO Orders...
                DB-->>Context: Success
                Seeder->>Logger: Log "Seeded OrderDb"
            else Data Already Exists
                Context-->>Seeder: true
                Seeder->>Logger: Log "Already has data"
            end

            HostExtension->>Logger: Log "Migrated successfully"
            HostExtension-->>Program: Success
        else Migration Fails
            Context-->>HostExtension: Exception (e.g., SQL connection failed)
            HostExtension->>Logger: Log error
            HostExtension->>HostExtension: Wait 2 seconds
            Note over HostExtension: Retry (up to 50 attempts)
        end
    end

    Program->>Program: host.Run()
```

## 7. Event Sourcing - Create Order Flow (Advanced Implementation)

```mermaid
sequenceDiagram
    participant Client
    participant Handler as CreateOrderHandlerES
    participant Repository as EventSourcedRepository
    participant OrderES as OrderES Aggregate
    participant EventStore as CosmosDbEventStore
    participant CosmosDB

    Client->>Handler: CreateOrderCommand

    Handler->>OrderES: OrderES.Create(...)
    Note over OrderES: Factory method

    OrderES->>OrderES: Validate invariants
    OrderES->>OrderES: Create OrderCreatedEventES
    OrderES->>OrderES: ApplyEvent(event, isNew: true)

    OrderES->>OrderES: Apply(OrderCreatedEventES)
    Note over OrderES: Set initial state
    OrderES->>OrderES: Add to uncommitted events

    OrderES-->>Handler: OrderES instance

    Handler->>Repository: SaveAsync(orderES)

    Repository->>OrderES: GetUncommittedEvents()
    OrderES-->>Repository: List<OrderCreatedEventES>

    Repository->>EventStore: SaveEventsAsync(aggregateId, events, expectedVersion)

    EventStore->>EventStore: Get current version
    EventStore->>CosmosDB: Query by AggregateId
    CosmosDB-->>EventStore: Current version

    alt Concurrency Conflict
        EventStore->>EventStore: expectedVersion != currentVersion
        EventStore-->>Repository: ConcurrencyException
        Repository-->>Handler: ConcurrencyException
        Handler-->>Client: 409 Conflict
    else No Conflict
        loop For each event
            EventStore->>EventStore: Serialize event to JSON
            EventStore->>EventStore: Create EventStoreEvent
            Note over EventStore: AggregateId, EventType, EventData, Version++
            EventStore->>CosmosDB: Insert document
        end

        CosmosDB-->>EventStore: Success
        EventStore-->>Repository: Success

        Repository->>OrderES: MarkEventsAsCommitted()
        OrderES->>OrderES: Clear uncommitted events

        Repository-->>Handler: Success
        Handler-->>Client: 200 OK (Order ID)
    end
```

## 8. Event Sourcing - Load Order from Events Flow

```mermaid
sequenceDiagram
    participant Client
    participant Handler as UpdateOrderHandlerES
    participant Repository as EventSourcedRepository
    participant EventStore as CosmosDbEventStore
    participant CosmosDB
    participant OrderES as OrderES Aggregate

    Client->>Handler: UpdateOrderCommand

    Handler->>Repository: GetByIdAsync(orderId)

    Repository->>EventStore: GetEventsAsync(orderId)
    EventStore->>CosmosDB: Query WHERE AggregateId = @id ORDER BY Version
    CosmosDB-->>EventStore: List<EventStoreEvent> documents

    EventStore->>EventStore: Deserialize events
    Note over EventStore: JsonConvert with TypeNameHandling.All
    EventStore-->>Repository: List<IDomainEvent>
    Note over EventStore,Repository: [OrderCreatedEventES, OrderItemAddedEvent, ...]

    Repository->>Repository: Create OrderES instance (reflection)
    Repository->>OrderES: LoadFromHistory(events)

    loop For each event
        OrderES->>OrderES: ApplyEvent(event, isNew: false)
        OrderES->>OrderES: Find Apply(EventType) method

        alt OrderCreatedEventES
            OrderES->>OrderES: Apply(OrderCreatedEventES)
            Note over OrderES: Set Id, CustomerId, OrderName, etc.
        else OrderItemAddedEvent
            OrderES->>OrderES: Apply(OrderItemAddedEvent)
            Note over OrderES: Add item to Items list
        else OrderUpdatedEventES
            OrderES->>OrderES: Apply(OrderUpdatedEventES)
            Note over OrderES: Update fields, status
        end

        OrderES->>OrderES: Increment Version
    end

    OrderES-->>Repository: Rebuilt OrderES aggregate
    Repository-->>Handler: OrderES with current state

    Handler->>OrderES: Update(orderName, address, ...)
    OrderES->>OrderES: Create OrderUpdatedEventES
    OrderES->>OrderES: ApplyEvent(event, isNew: true)
    OrderES->>OrderES: Apply(OrderUpdatedEventES)
    OrderES->>OrderES: Add to uncommitted events

    Handler->>Repository: SaveAsync(orderES)
    Note over Handler,Repository: Same as Create flow - saves new events

    Repository-->>Handler: Success
    Handler-->>Client: 200 OK
```

## 9. MediatR Pipeline Behavior Flow

```mermaid
sequenceDiagram
    participant Client
    participant MediatR
    participant UnhandledBehaviour as UnhandledExceptionBehaviour
    participant ValidationBehaviour
    participant ActualHandler as Command/Query Handler

    Client->>MediatR: Send(TRequest)

    MediatR->>UnhandledBehaviour: Handle(request, next)

    UnhandledBehaviour->>ValidationBehaviour: await next()

    ValidationBehaviour->>ValidationBehaviour: Get all validators for TRequest

    alt No Validators Found
        ValidationBehaviour->>ActualHandler: await next()
    else Validators Found
        ValidationBehaviour->>ValidationBehaviour: Run all validators
        ValidationBehaviour->>ValidationBehaviour: Collect failures

        alt Validation Failures Exist
            ValidationBehaviour->>ValidationBehaviour: Create ValidationException
            ValidationBehaviour-->>UnhandledBehaviour: throw ValidationException
            UnhandledBehaviour-->>MediatR: ValidationException
            MediatR-->>Client: 400 Bad Request
        else All Valid
            ValidationBehaviour->>ActualHandler: await next()

            alt Handler Throws Exception
                ActualHandler-->>ValidationBehaviour: Exception
                ValidationBehaviour-->>UnhandledBehaviour: Exception

                UnhandledBehaviour->>UnhandledBehaviour: Log exception details
                Note over UnhandledBehaviour: Log request name, exception
                UnhandledBehaviour-->>MediatR: Re-throw exception
                MediatR-->>Client: 500 Internal Server Error
            else Handler Succeeds
                ActualHandler-->>ValidationBehaviour: TResponse
                ValidationBehaviour-->>UnhandledBehaviour: TResponse
                UnhandledBehaviour-->>MediatR: TResponse
                MediatR-->>Client: Success response
            end
        end
    end
```

## 10. Repository Pattern - Query with Includes and Ordering

```mermaid
sequenceDiagram
    participant Handler as Query Handler
    participant Repository as RepositoryBase<T>
    participant Context as DbContext
    participant DB as SQL Server

    Handler->>Repository: GetAsync(predicate, orderBy, includes, disableTracking)
    Note over Handler,Repository: predicate: o => o.UserName == "john"<br/>orderBy: o => o.OrderByDescending(x => x.CreatedDate)<br/>includes: "OrderItems"<br/>disableTracking: true

    Repository->>Context: Set<T>
    Context-->>Repository: DbSet<Order>

    Repository->>Repository: query = dbSet

    alt disableTracking = true
        Repository->>Repository: query = query.AsNoTracking()
    end

    alt includes (string) provided
        Repository->>Repository: query = query.Include("OrderItems")
    end

    alt predicate provided
        Repository->>Repository: query = query.Where(predicate)
    end

    alt orderBy provided
        Repository->>Repository: query = orderBy(query)
        Note over Repository: query.OrderByDescending(x => x.CreatedDate)
    end

    Repository->>Context: query.ToListAsync()
    Context->>DB: Generate SQL with JOINs, WHERE, ORDER BY
    Note over Context,DB: SELECT * FROM Orders o<br/>LEFT JOIN OrderItems oi ON o.Id = oi.OrderId<br/>WHERE o.UserName = @p0<br/>ORDER BY o.CreatedDate DESC

    DB-->>Context: Result rows
    Context->>Context: Materialize entities
    Context-->>Repository: List<Order>
    Repository-->>Handler: List<Order>
```

---

## Legend

- **Solid arrows (→)**: Synchronous calls
- **Dashed arrows (-->>)**: Return values/responses
- **Note boxes**: Additional context or important details
- **alt/else blocks**: Conditional flows
- **loop blocks**: Iterative processes

## Tools for Viewing

These Mermaid diagrams can be viewed in:
- GitHub (native support)
- Visual Studio Code (with Mermaid extension)
- Online: https://mermaid.live/
- Any Markdown viewer with Mermaid support
