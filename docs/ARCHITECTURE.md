# Architecture

## Shape

DemoTradeLab is a modular monolith. All modules are built and deployed together, while project references enforce clear responsibilities.

```text
HTTP client
    |
    v
DemoTradeLab.Api
    |              \
    v               v
DemoTradeLab.Core <- DemoTradeLab.Infrastructure
```

`DemoTradeLab.Api` references both Core and Infrastructure. Infrastructure references Core. Core has no project references and remains independent of HTTP and persistence concerns.

## Project responsibilities

### DemoTradeLab.Api

- Hosts controller-based HTTP endpoints.
- Defines request and response contracts.
- Configures dependency injection, middleware, OpenAPI, and logging.
- Converts application outcomes into HTTP responses and Problem Details.

### DemoTradeLab.Core

- Contains domain models, business rules, interfaces, and use cases.
- Uses no EF Core or ASP.NET Core types.
- Holds precision-sensitive financial values as `decimal` and timestamps as UTC `DateTimeOffset` values.

The `Trade` entity uses a private constructor and a public `Create` factory. Callers provide a `TradeDraft`, and creation returns either a valid `Trade` or a collection of structured validation errors. This prevents partially valid entities from entering later application and persistence flows.

### DemoTradeLab.Infrastructure

- Contains the EF Core `DemoTradeLabDbContext`, SQLite entity configuration, and migrations.
- Implements focused Core repositories for trades and demo profiles through EF Core.
- Is wired into the application by Api through the `AddInfrastructure` dependency-injection extension.

### Tests

- Unit tests exercise isolated Core business rules.
- Integration tests exercise the ASP.NET Core application through a test host and an isolated temporary SQLite database.

### demotrade-lab-web

- Hosts the React and TypeScript single-page dashboard built with Vite.
- Defines browser-facing API response types and a small `fetch` client.
- Keeps request lifecycle logic in custom hooks and rendering in presentational components.
- Treats Core analytics as authoritative instead of recalculating monetary results in JavaScript.

## Current request flow

`GET /api/health` reaches `HealthController`. The controller asks ASP.NET Core's `HealthCheckService` for the aggregate application status and returns an explicit JSON response containing the status and UTC check time.

## Current domain flow

```text
TradeDraft -> Trade.Create -> TradeCreationResult
                                |-- valid Trade
                                `-- validation errors
```

The domain layer normalizes instrument and currency text, validates completed-trade invariants, and generates the internal `Guid` identity only after validation succeeds.

## Persistence flow

```text
Api configuration
    |
    v
Validated demo options + AddInfrastructure(connection string, seed definitions)
    |
    v
DemoTradeLabDbContext -> EF Core SQLite provider -> local SQLite database
```

Explicit Infrastructure configurations map `Trade`, `DemoProfile`, and `DemoAccount` without adding EF Core attributes to Core. Enums are stored as readable strings. Financial values remain `decimal`; the SQLite provider stores them as text so their decimal representation survives a round trip without conversion to binary floating point.

Schema changes are represented by committed EF Core migrations. The API does not call `Database.Migrate()` during startup; developers apply migrations explicitly with the repository-local `dotnet-ef` tool.

## Sample-data seeding

EF Core's `UseSeeding` and `UseAsyncSeeding` hooks populate an empty `Trades` table when migrations are explicitly applied. Both paths use the normal `Trade.Create` domain factory, so sample data cannot bypass business validation. The seeder exits when any trade already exists, which makes repeated database-update commands idempotent for the initial dataset.

The eight records are fixed, fictional examples marked with `TradeDataSource.Sample`. They intentionally produce five profitable trades, three losing trades, and a total realized profit/loss of `124 USD`, providing a known dataset for upcoming CRUD and analytics work.

## Configurable demo-environment flow

```text
demo-environment.json
    |
    v
DemoEnvironmentOptions startup validation
    |
    v
DemoProfileSeed definitions -> EF migration-aware seeding
    |
    v
DemoProfiles + DemoAccounts in SQLite -> GET /api/demo-profiles
```

Configuration contains fictional initialization values only and no authentication data. Core factories normalize keys and validate names, currencies, balances, and duplicate accounts. Database unique indexes protect profile keys and account keys within a profile as a second line of defense.

Initialization inserts a configured profile or account only when its normalized key is missing. It never updates an existing record, so persisted balances and future actions survive later migration commands or configuration changes. `TotalBalance` and `ReservedBalance` are stored; `AvailableBalance` is calculated as their difference. The read controller returns explicit DTOs and does not expose EF entities.

## Trade CRUD flow

```text
HTTP request
    |
    v
SaveTradeRequest and automatic API validation
    |
    v
TradesController
    |
    v
TradeService -> Trade domain validation
    |
    v
ITradeRepository
    |
    v
EfTradeRepository -> DemoTradeLabDbContext -> SQLite
```

Api DTOs are mapped explicitly instead of returning EF/domain entities. `TradeService` coordinates the use case without referencing ASP.NET Core or EF Core, while the focused repository hides query tracking and persistence mechanics. Updates preserve the entity ID, data source, and import timestamp. Domain validation errors become HTTP 400 Validation Problem Details; missing resources become HTTP 404 Problem Details.

## Analytics and trade-query flow

```text
HTTP query or analytics request
    |
    v
API request/response DTOs and controller
    |
    v
TradeService or TradeAnalyticsService
    |
    v
ITradeRepository -> read-only trade snapshot
    |
    v
Core filtering/sorting or TradeAnalyticsCalculator
```

`TradeService` applies validated filters and deterministic ordering for the trade table. `TradeAnalyticsCalculator` is a pure Core component: it has no database, HTTP, clock, or logging dependency, so calculation edge cases are covered with fast unit tests.

For the bounded local MVP, the repository loads a read-only trade snapshot and Core performs filtering, decimal aggregation, and timeline construction in memory. This preserves exact `decimal` behavior despite SQLite's server-side decimal query limitations. It is not the intended design for an unbounded production dataset; pagination and database-specific aggregation would be introduced before scaling the data volume.

Counts and durations can span all trades, but money cannot be meaningfully added across currencies. Dashboard performance, best/worst trades, instrument totals, and profit/loss timelines are therefore partitioned by currency. Instrument names are grouped case-insensitively, and deterministic tie-breakers keep responses stable.

## Frontend data flow

```text
Browser at localhost:5173
    |
    v
React components -> custom data hooks -> typed fetch client
    |                                      |
    |                                      v
    `---------------- Vite /api proxy -> ASP.NET Core at localhost:5122
```

The overview hook loads dashboard statistics, instrument summaries, and timeline data concurrently. The trade-list hook sends filters and sorting to `GET /api/trades`; selecting a row separately calls `GET /api/trades/{id}`. Every effect owns an `AbortController`, so unmounting or changing filters cancels obsolete requests.

Components render explicit loading, error, empty, and success states. The table and detail panel form a responsive workspace on larger screens and stack on smaller screens. The timeline uses a code-native SVG rather than adding a chart dependency for one simple series.

Vite proxies relative `/api` requests during development. This avoids weakening the backend with a broad CORS policy. A separately deployed frontend can provide an absolute `VITE_API_BASE_URL` at build time.

ASP.NET Core serializes `decimal` values as JSON numbers, which browsers parse as JavaScript `number` values. The frontend therefore uses them for display and chart coordinates only; it never recomputes authoritative financial aggregates. A future requirement for client-side financial arithmetic would need a string-based decimal API contract or a decimal library.

## Planned reliability-simulator architecture

The `DemoProfile` and `DemoAccount` foundation is implemented in Milestone 4. Reservation transitions, transaction orchestration, idempotency, audit, order, and locking components remain Milestone 5 design targets.

```text
POST /api/accounts/{accountId}/reservations
    |
    v
AccountsController and request DTO validation
    |
    v
ReservationService
    |
    +--> IAccountLockManager.AcquireAsync(accountId)
    |        `-- local keyed SemaphoreSlim implementation for the first lesson
    |
    v
IAccountRepository and IReservationRepository
    |
    v
EF Core explicit transaction -> SQLite
```

Core already owns the `DemoAccount` model and demo-profile read use case. It will also own protected balance transitions, the reservation lifecycle, use-case result types, and lock interfaces. Api will own headers, DTOs, status-code mapping, Problem Details, and structured request logging. Infrastructure already owns account persistence and will own transaction execution, durable reservation/idempotency/audit persistence, and the clearly named local lock implementation.

`AvailableBalance` is calculated as `TotalBalance - ReservedBalance`, keeping one source of truth instead of persisting three values that can disagree. Milestone 5 state-transition methods must reject any operation that would make reserved balance negative, greater than total balance, or available balance negative.

### Planned critical section

```text
Acquire the lock for one account
    Begin the database transaction
    Look up the durable idempotency key
    Load the authoritative account state
    Validate available balance
    Increase reserved balance
    Create the active reservation
    Create the audit event
    Save changes and commit
Release the lock through an async-disposable lease
```

The lock must be released by `await using`/`DisposeAsync` even when cancellation or an exception occurs. External calls, notifications, artificial sleeps, and long calculations do not belong inside the critical section. Consuming or releasing a reservation later will be a separate idempotent, locked transaction.

### Planned concurrent request sequence

```text
Request A       Per-account lock       Database       Request B
    | acquire(account) |                  |               |
    |----------------->|                  |               |
    |<-- lease granted |                  |               |
    |                  |                  | acquire(account)
    |                  |<---------------------------------|
    |                  |                  | waits         |
    | begin/read available=100            |               |
    |------------------------------------>|               |
    | reserve 80; commit                  |               |
    |------------------------------------>|               |
    | release lease    |                  |               |
    |----------------->|                  |               |
    |                  |------------------------------->  |
    |                  |                  | lease granted |
    |                  |                  | read 20       |
    |                  |                  | reject 80     |
```

The expected final state is total `100`, reserved `80`, and available `20`. A retry with the successful request's idempotency key waits for the same account operation when necessary, then reads the committed idempotency/reservation record and returns the original result without reserving again. A database uniqueness constraint remains the durable backstop.

### Locking semantics and limitations

- C# `lock`/`Monitor` are synchronous, re-entrant, process-local primitives and cannot safely wrap awaited work.
- `SemaphoreSlim.WaitAsync` supports asynchronous waiting, but it is still process-local and is not a database or distributed lock.
- A keyed lock avoids one global application lock: Account A and Account B receive different semaphores. SQLite may nevertheless serialize their writes at the database layer.
- The initial operation acquires exactly one account lock, eliminating lock-order cycles. If a future transfer needs multiple accounts, it must sort account IDs into one global acquisition order and release every lease safely; callers must not choose their own order.
- Cancellation-aware waiting and async-disposable leases prevent abandoned locks. Lock waits should be observable, but retry loops must not spin aggressively.
- SQLite does not offer the same row-level pessimistic locking or `SELECT FOR UPDATE` behavior as common server databases. The design must not pretend otherwise.
- Multiple application instances each have a different in-memory lock manager. Therefore the first educational implementation is correct only for the documented single-instance mode.
- A production-style multi-instance variant requires a provider that supports genuine row locks or another explicitly designed distributed coordination mechanism, plus cross-instance tests.

## Deferred design

The reliability simulator now has an authoritative persisted account foundation. Its reservation, idempotency, audit, transaction, and locking types remain deferred until Milestone 5 so they are introduced together with the invariants they must protect.
