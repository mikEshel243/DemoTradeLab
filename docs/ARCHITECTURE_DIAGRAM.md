# Architecture Diagram

## Modular monolith and request boundaries

```mermaid
flowchart LR
    Browser[React dashboard] -->|HTTP JSON| Api
    RestClient[REST client or integration test] -->|HTTP JSON| Api

    subgraph Api[DemoTradeLab.Api]
        Controllers[Controllers and DTO validation]
        Composition[Dependency injection and configuration]
    end

    subgraph Core[DemoTradeLab.Core]
        Services[Application services]
        Domain[Trade, account, reservation, and order domain models]
        Ports[Repository, transaction, and lock interfaces]
    end

    subgraph Infrastructure[DemoTradeLab.Infrastructure]
        Repositories[EF Core repositories]
        Lock[Keyed local SemaphoreSlim lock]
        Transactions[EF transaction adapter]
        Seeder[Migration-aware fictional seeding]
    end

    Database[(Local SQLite database)]

    Controllers --> Services
    Composition --> Controllers
    Services --> Domain
    Services --> Ports
    Ports -. implemented by .-> Repositories
    Ports -. implemented by .-> Lock
    Ports -. implemented by .-> Transactions
    Repositories --> Database
    Transactions --> Database
    Seeder --> Database
```

Core has no reference to Api or Infrastructure. Api owns HTTP and configuration. Infrastructure owns technology-specific implementations.

## Atomic reservation creation

```mermaid
sequenceDiagram
    participant C as HTTP client
    participant A as ReservationsController
    participant S as ReservationService
    participant L as Account lock
    participant R as EF repository
    participant D as SQLite

    C->>A: POST reservation + Idempotency-Key
    A->>S: CreateAsync
    S->>L: AcquireAsync(accountId)
    L-->>S: async lease
    S->>R: BeginTransactionAsync
    S->>R: Find durable idempotency record
    alt previous outcome exists
        R-->>S: stored success or rejection
        S-->>A: replay result
    else first request
        S->>R: Load tracked account
        S->>S: Validate available balance
        S->>R: Add reservation, idempotency, audit
        R->>D: SaveChanges and commit
        S-->>A: created or insufficient funds
    end
    S->>L: DisposeAsync lease
    A-->>C: HTTP result
```

## Reservation and order state machines

```mermaid
stateDiagram-v2
    [*] --> Active: reserve funds
    Active --> Released: release or compensate
    Active --> Consumed: consume or complete order
    Released --> [*]
    Consumed --> [*]
```

```mermaid
stateDiagram-v2
    [*] --> Pending: create from active reservation
    Pending --> Completed: consume reservation
    Pending --> Failed: simulated later failure
    Failed --> Compensated: release reservation
    Completed --> [*]
    Compensated --> [*]
```

## Safety boundary

The lock coordinates requests only inside one application process. SQLite transactions and uniqueness constraints provide durable atomicity and idempotency, but the local semaphore is not a distributed lock. A multi-instance variant requires a different coordination design and separate cross-instance verification.
