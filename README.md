# DemoTradeLab

DemoTradeLab is a portfolio and interview-preparation project for learning C# and ASP.NET Core through an educational demo-trading analytics application and, in a later milestone, a backend reliability simulator.

This is an unofficial educational project. It is not affiliated with, endorsed by, or connected to Plus500 or any other financial institution. It never connects to real trading accounts, accepts brokerage credentials, submits real orders, or provides automated trading recommendations.

## Current scope

The repository foundation, trade CRUD backend, and Milestone 2 analytics API are complete:

- .NET 10 modular-monolith solution
- Controller-based ASP.NET Core Web API
- Core and Infrastructure class libraries
- xUnit unit and integration test projects
- OpenAPI document generation in Development
- `GET /api/health` health endpoint
- Completed-trade domain model with explicit validation results
- Unit tests for trade invariants and normalization
- EF Core 10 with SQLite persistence
- Initial `Trades` database migration
- Integration test covering migration, save, and reload
- Eight fictional sample trades seeded into a new empty database
- Trade create, read, update, and delete endpoints
- Structured validation and not-found Problem Details
- Filterable and sortable trade listing
- Dashboard statistics, instrument summaries, and profit/loss timeline endpoints
- Currency-separated monetary analytics

Importing, the React frontend, and the reliability simulator are intentionally deferred to later milestones.

## Prerequisites

- .NET 10 SDK
- Git

The foundation was created and verified with .NET SDK 10.0.302.

## Build and test

From the repository root:

```powershell
dotnet tool restore
dotnet restore DemoTradeLab.sln
dotnet build DemoTradeLab.sln
dotnet test DemoTradeLab.sln
```

`dotnet tool restore` installs the repository-pinned `dotnet-ef` command. It does not install a machine-global tool.

## Create or update the local database

Apply all committed EF Core migrations:

```powershell
dotnet ef database update `
  --project src/DemoTradeLab.Infrastructure `
  --startup-project src/DemoTradeLab.Api
```

The default SQLite database is `demotrade-lab.db`. Database files are local development artifacts and are ignored by Git.

Database update also seeds eight fictional sample trades when the `Trades` table is empty. Re-running the command does not duplicate them.

Migrations are not applied automatically during API startup. This keeps schema changes explicit and prevents an application instance from unexpectedly changing a database.

## Run the API

```powershell
dotnet run --project src/DemoTradeLab.Api
```

With the default HTTP launch profile, request:

```http
GET http://localhost:5122/api/health
```

Example response:

```json
{
  "status": "Healthy",
  "checkedAtUtc": "2026-08-04T10:00:00+00:00"
}
```

In Development, the OpenAPI document is available at `/openapi/v1.json`.

## Trade API

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/trades` | Lists trades |
| `GET` | `/api/trades/{id}` | Returns one trade or 404 Problem Details |
| `POST` | `/api/trades` | Creates a manual trade and returns 201 |
| `PUT` | `/api/trades/{id}` | Replaces editable trade fields |
| `DELETE` | `/api/trades/{id}` | Deletes a trade and returns 204 |

Create and update requests accept string enum values such as `"buy"` and `"sell"`. New API-created trades always receive the `manual` source; clients cannot claim that a record came from sample or imported data.

See `src/DemoTradeLab.Api/DemoTradeLab.Api.http` for an executable request example.

`GET /api/trades` accepts these optional query parameters:

| Parameter | Values or meaning |
| --- | --- |
| `instrument` | Exact instrument match, case-insensitive |
| `currency` | Three-letter currency code, case-insensitive |
| `direction` | `buy` or `sell` |
| `source` | `manual`, `sample`, or `imported` |
| `outcome` | `profitable`, `losing`, or `breakEven` |
| `closedFromUtc` | Inclusive UTC closing-time lower bound |
| `closedToUtc` | Inclusive UTC closing-time upper bound |
| `sortBy` | `closedAtUtc`, `openedAtUtc`, `instrument`, `realizedProfitLoss`, or `duration` |
| `sortDirection` | `ascending` or `descending` |

The default order is newest closing time first. Example:

```http
GET /api/trades?instrument=EUR%2FUSD&outcome=profitable&sortBy=realizedProfitLoss&sortDirection=descending
```

## Analytics API

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/analytics/dashboard` | Counts, win rate, most active instrument, average duration, and currency performance |
| `GET` | `/api/analytics/instruments` | Statistics grouped by instrument and currency |
| `GET` | `/api/analytics/profit-loss-timeline` | Chronological and cumulative realized profit/loss points per currency |

Win rate is profitable trades divided by all completed trades, including break-even trades in the denominator. Monetary values are never summed across different currencies. Best trade, worst trade, total realized profit/loss, and timeline totals are therefore separated by currency.

Analytics currently use `RealizedProfitLoss` as stored. Optional fees and financing costs are exposed separately on a trade and are not subtracted a second time.

## Repository structure

```text
DemoTradeLab/
|-- src/
|   |-- DemoTradeLab.Api/
|   |-- DemoTradeLab.Core/
|   `-- DemoTradeLab.Infrastructure/
|-- tests/
|   |-- DemoTradeLab.UnitTests/
|   `-- DemoTradeLab.IntegrationTests/
|-- docs/
|-- AGENTS.md
|-- README.md
`-- DemoTradeLab.sln
```

See [Architecture](docs/ARCHITECTURE.md), [Decisions](docs/DECISIONS.md), and [Roadmap](docs/ROADMAP.md) for more detail.
