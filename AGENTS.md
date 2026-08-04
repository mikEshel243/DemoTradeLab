# Repository Instructions

## Scope and safety

- DemoTradeLab is an unofficial educational project. Never imply affiliation with Plus500 or any financial institution.
- Use only fictional, manually entered, or anonymized demo data.
- Never request brokerage credentials, connect to live trading APIs, submit orders, scrape broker platforms, or add trading recommendations.
- Do not implement a Plus500-specific importer until an anonymized sample is supplied. Treat every imported file as untrusted.
- Work one roadmap milestone at a time and avoid unrelated refactors.

## Architecture

- Keep the application a modular monolith.
- `DemoTradeLab.Api` owns HTTP endpoints, DTOs, and application configuration.
- `DemoTradeLab.Core` owns domain models, business rules, interfaces, and use cases. It must not reference Infrastructure or Api.
- `DemoTradeLab.Infrastructure` owns EF Core, SQLite, repositories, and other implementations. It may reference Core.
- Controllers must remain thin and must not return EF Core entities.
- Do not add mediator, mapping, or generic-repository libraries without a concrete need.

## C# and API conventions

- Target .NET 10 with nullable reference types enabled.
- Use `decimal` for prices, money, and quantities where precision matters.
- Store timestamps in UTC and use async I/O with cancellation tokens where appropriate.
- Validate request DTOs at the API boundary and use Problem Details for HTTP errors.
- Prefer expected result types over exceptions for business rejections.
- Use built-in dependency injection and structured logging.

## Verification and documentation

- Before finishing a change, run `dotnet format DemoTradeLab.sln`, `dotnet build DemoTradeLab.sln`, and `dotnet test DemoTradeLab.sln` as applicable.
- Restore repository-local tools with `dotnet tool restore`.
- Keep EF Core migrations in `DemoTradeLab.Infrastructure/Persistence/Migrations` and use `DemoTradeLab.Api` as the startup project.
- Do not apply migrations automatically during normal API startup.
- Run frontend lint and build only after the frontend exists.
- Update README and relevant files under `docs/` whenever setup, behavior, architecture, decisions, or roadmap status changes.
- Never commit local databases, uploaded reports, secrets, credentials, or personal financial data.
