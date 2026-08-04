# Architectural Decisions

## ADR-001: Use a modular monolith

- **Status:** Accepted
- **Decision:** Build one deployable ASP.NET Core application split into Api, Core, and Infrastructure projects.
- **Reason:** It demonstrates separation of concerns without the operational cost or distributed failure modes of premature microservices.

## ADR-002: Keep dependencies directed toward Core

- **Status:** Accepted
- **Decision:** Core has no references to Api or Infrastructure. Infrastructure references Core; Api composes both.
- **Reason:** Business rules remain testable without HTTP or database dependencies. This is comparable to keeping a Spring domain/application module independent of controllers and JPA adapters.

## ADR-003: Use controller-based APIs

- **Status:** Accepted
- **Decision:** Expose HTTP operations through ASP.NET Core controllers and explicit DTOs.
- **Reason:** Controllers are a project requirement and provide a familiar transition from Spring MVC's `@RestController` model.

## ADR-004: Use the built-in health-check service

- **Status:** Accepted
- **Decision:** Implement `GET /api/health` as a controller backed by ASP.NET Core `HealthCheckService`.
- **Reason:** The endpoint stays controller-based while allowing database and dependency checks to be registered later without changing its contract.

## ADR-005: Retain the existing `.sln` format

- **Status:** Accepted
- **Decision:** Keep the valid `DemoTradeLab.sln` created in the initial repository commit rather than replacing it with `.slnx` during foundation work.
- **Reason:** Both formats are supported by .NET 10, and changing a working solution file adds no Milestone 0 capability.

## ADR-006: Pin a patched OpenAPI.NET release

- **Status:** Accepted
- **Decision:** Pin `Microsoft.OpenApi` to the latest compatible 2.x release in addition to the ASP.NET Core OpenAPI package.
- **Reason:** The ASP.NET Core package's minimum transitive dependency resolved to a version with a high-severity denial-of-service advisory. An explicit 2.x pin keeps the dependency on the patched major line without introducing a major-version compatibility change.

## ADR-007: Create trades through an explicit validation result

- **Status:** Accepted
- **Decision:** Keep the `Trade` constructor private and create trades from a `TradeDraft` through `Trade.Create`, returning `TradeCreationResult`.
- **Reason:** A successful entity always satisfies its domain invariants, while ordinary invalid input is reported as structured data instead of exceptions. The approach is intentionally specific to trade creation rather than introducing a generic result framework prematurely.

## ADR-008: Represent instants with UTC `DateTimeOffset`

- **Status:** Accepted
- **Decision:** Require zero-offset `DateTimeOffset` values for opening, closing, and import timestamps.
- **Reason:** `DateTimeOffset` represents an unambiguous instant, and enforcing UTC gives storage and comparison consistency. User-local time conversion belongs at the presentation boundary.

## ADR-009: Keep EF Core mapping in Infrastructure

- **Status:** Accepted
- **Decision:** Configure `Trade` through `IEntityTypeConfiguration<Trade>` in Infrastructure instead of placing EF Core attributes on the domain entity.
- **Reason:** Core remains independent of persistence technology, while the explicit mapping keeps database column requirements visible and testable.

## ADR-010: Use explicit migrations and a local EF tool

- **Status:** Accepted
- **Decision:** Commit EF Core migrations, pin `dotnet-ef` in the repository tool manifest, and require an explicit database-update command.
- **Reason:** Reproducible tooling avoids machine-specific versions. Explicit migration execution makes schema changes deliberate and is safer than changing the database automatically whenever the API starts.

## ADR-011: Preserve decimal values with the SQLite provider

- **Status:** Accepted
- **Decision:** Keep financial properties as `decimal` even though SQLite stores them using its text representation.
- **Reason:** Converting financial values to `double` would introduce binary floating-point error. SQLite's limitations for server-side decimal ordering and aggregation will be considered explicitly during the analytics milestone.

## ADR-012: Pin a patched native SQLite bundle

- **Status:** Accepted
- **Decision:** Pin `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 alongside the EF Core SQLite provider.
- **Reason:** EF Core 10.0.10 otherwise resolved bundle 2.1.11, whose native SQLite dependency has a high-severity advisory. Staying on patched 2.1.12 avoids an unnecessary major-version jump.

## ADR-013: Seed sample data through migration-aware hooks

- **Status:** Accepted
- **Decision:** Use both `UseSeeding` and `UseAsyncSeeding` for the fictional initial dataset instead of model-managed `HasData` or ordinary API-startup code.
- **Reason:** EF's seeding hooks run under the migration lock and support database-state checks. The synchronous hook supports current EF tooling, while the asynchronous hook supports application migration calls and cancellation. Keeping seeding attached to explicit migration execution avoids database writes during normal API startup.

## ADR-014: Use a focused repository and application service

- **Status:** Accepted
- **Decision:** Define `ITradeRepository` and `TradeService` in Core, with one EF-specific repository implementation in Infrastructure.
- **Reason:** The controller remains limited to HTTP concerns, Core owns use-case orchestration, and EF tracking details stay in Infrastructure. A generic repository framework was intentionally avoided because it would mostly duplicate `DbSet` without expressing trade-specific needs.

## ADR-015: Keep HTTP contracts separate from domain entities

- **Status:** Accepted
- **Decision:** Accept `SaveTradeRequest` and return `TradeResponse`, using explicit mapping at the API boundary. Serialize trade enums as lowercase strings.
- **Reason:** API contracts can evolve independently from persistence and domain implementation. Readable enum strings improve the HTTP contract, while clients are prevented from setting protected provenance fields such as source and import timestamp.
