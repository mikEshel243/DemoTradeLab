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
- Will hold precision-sensitive financial values as `decimal` and timestamps as UTC values.

### DemoTradeLab.Infrastructure

- Will contain EF Core, SQLite configuration, repositories, and concrete external implementations.
- Implements interfaces defined by Core.
- Is wired into the application by Api through dependency injection.

### Tests

- Unit tests exercise isolated Core business rules.
- Integration tests exercise the ASP.NET Core application through an in-memory test host and, in later milestones, a controlled test database.

## Current request flow

`GET /api/health` reaches `HealthController`. The controller asks ASP.NET Core's `HealthCheckService` for the aggregate application status and returns an explicit JSON response containing the status and UTC check time.

## Deferred design

Trade persistence, analytics, imports, frontend integration, and reliability-simulator internals will be designed in their own milestones. This avoids inventing abstractions before their requirements are exercised.
