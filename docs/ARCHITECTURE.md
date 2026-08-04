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
- Implements the focused Core `ITradeRepository` through `EfTradeRepository`.
- Is wired into the application by Api through the `AddInfrastructure` dependency-injection extension.

### Tests

- Unit tests exercise isolated Core business rules.
- Integration tests exercise the ASP.NET Core application through a test host and an isolated temporary SQLite database.

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
AddInfrastructure(connection string)
    |
    v
DemoTradeLabDbContext -> EF Core SQLite provider -> local SQLite database
```

`TradeConfiguration` maps the domain entity without adding EF Core attributes to Core. Enums are stored as readable strings. Financial values remain `decimal`; the SQLite provider stores them as text so their decimal representation survives a round trip without conversion to binary floating point.

Schema changes are represented by committed EF Core migrations. The API does not call `Database.Migrate()` during startup; developers apply migrations explicitly with the repository-local `dotnet-ef` tool.

## Sample-data seeding

EF Core's `UseSeeding` and `UseAsyncSeeding` hooks populate an empty `Trades` table when migrations are explicitly applied. Both paths use the normal `Trade.Create` domain factory, so sample data cannot bypass business validation. The seeder exits when any trade already exists, which makes repeated database-update commands idempotent for the initial dataset.

The eight records are fixed, fictional examples marked with `TradeDataSource.Sample`. They intentionally produce five profitable trades, three losing trades, and a total realized profit/loss of `124 USD`, providing a known dataset for upcoming CRUD and analytics work.

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

## Deferred design

Analytics, imports, frontend integration, and reliability-simulator internals will be designed in their own milestones. This avoids inventing abstractions before their requirements are exercised.
