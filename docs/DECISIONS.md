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
