# DemoTradeLab

DemoTradeLab is a portfolio and interview-preparation project for learning C# and ASP.NET Core through an educational demo-trading analytics application and, in a later milestone, a backend reliability simulator.

This is an unofficial educational project. It is not affiliated with, endorsed by, or connected to Plus500 or any other financial institution. It never connects to real trading accounts, accepts brokerage credentials, submits real orders, or provides automated trading recommendations.

## Current scope

Milestone 0 provides the backend repository foundation:

- .NET 10 modular-monolith solution
- Controller-based ASP.NET Core Web API
- Core and Infrastructure class libraries
- xUnit unit and integration test projects
- OpenAPI document generation in Development
- `GET /api/health` health endpoint

Trade storage, analytics, importing, the React frontend, and the reliability simulator are intentionally deferred to later milestones.

## Prerequisites

- .NET 10 SDK
- Git

The foundation was created and verified with .NET SDK 10.0.302.

## Build and test

From the repository root:

```powershell
dotnet restore DemoTradeLab.sln
dotnet build DemoTradeLab.sln
dotnet test DemoTradeLab.sln
```

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
