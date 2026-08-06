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

## ADR-016: Calculate bounded MVP analytics in Core

- **Status:** Accepted
- **Decision:** Load the current read-only trade set and perform filtering, sorting, decimal aggregation, and timeline calculations in Core for Milestone 2.
- **Reason:** SQLite does not support every required server-side operation over .NET `decimal` values. In-memory calculation preserves financial precision and keeps the rules independently unit-testable for the intentionally small local dataset. This decision must be revisited with pagination and a database-appropriate query strategy before supporting unbounded data.

## ADR-017: Keep monetary analytics separated by currency

- **Status:** Accepted
- **Decision:** Return total profit/loss, best and worst trades, instrument totals, and cumulative timelines per currency rather than adding different currencies together.
- **Reason:** Adding values such as USD and EUR without an explicit exchange-rate source produces a financially meaningless result. DemoTradeLab has no market-data or currency-conversion dependency, so currency separation is the honest contract.

## ADR-018: Define deterministic analytics rules

- **Status:** Accepted
- **Decision:** Calculate win rate as profitable trades divided by all completed trades, classify exactly zero profit/loss as break-even, round percentage results to two decimal places, and use explicit alphabetical/time/ID tie-breakers.
- **Reason:** Analytics are easier to trust, test, and explain when edge-case behavior does not depend on database row order or unstated conventions.

## ADR-019: Stage balance concurrency after its reservation prerequisites

- **Status:** Accepted
- **Decision:** Keep the lock-based balance-reservation exercise in Milestone 5. Reuse the persisted Milestone 4 `DemoAccount`, then build protected balance transitions, the reservation lifecycle, result types, transaction ownership, and idempotency before adding the concurrent operation.
- **Reason:** A lock is meaningful only when it protects an authoritative state transition. The durable balance now exists, but a standalone semaphore would still omit the reservation, transaction, idempotency, and failure-recovery behavior that the lesson is intended to demonstrate.

## ADR-020: Use a per-account local asynchronous lock for the first locking lesson

- **Status:** Accepted
- **Decision:** Define `IAccountLockManager` in Core and initially implement it in Infrastructure with keyed `SemaphoreSlim` instances. Hold the lease only around the authoritative read, invariant check, reservation/idempotency/audit writes, and explicit transaction commit. Always release it through an async-disposable lease.
- **Reason:** Unlike C# `lock`/`Monitor`, `SemaphoreSlim.WaitAsync` supports asynchronous EF Core work. Keying by account avoids a global application lock and makes the critical-section boundary visible for teaching and deterministic tests.
- **Limitation:** The implementation coordinates one application process only. It does not protect multiple server instances and is not a SQLite row lock. SQLite has database-level write-serialization behavior and no SQL Server/PostgreSQL-style `SELECT FOR UPDATE`. A multi-instance variant requires a genuine provider-specific or distributed strategy and separate verification.

## ADR-021: Make reservation idempotency durable and transactional

- **Status:** Accepted
- **Decision:** Persist the idempotency key with a uniqueness constraint and create its outcome, balance mutation, reservation, and audit record in the same transaction. A duplicate completed key returns the original outcome; a same-account duplicate still in progress waits on the account lock and then reads the committed result. Failed transactions leave no successful idempotency outcome and may be retried according to an explicit retry policy.
- **Reason:** An in-memory dictionary is lost on restart and cannot coordinate multiple processes. Keeping idempotency and the state transition in one transaction prevents a successful balance reservation from existing without its retry record, or vice versa.

## ADR-022: Use a Vite development proxy for the local frontend

- **Status:** Accepted
- **Decision:** Keep browser API paths relative and proxy `/api` from Vite's development server to the ASP.NET Core HTTP endpoint. Support `VITE_API_BASE_URL` for separately hosted builds.
- **Reason:** The local browser sees one origin, so the backend does not need a permissive development CORS policy. Environment-specific API addresses remain outside committed source code.

## ADR-023: Keep the first dashboard dependency-light

- **Status:** Accepted
- **Decision:** Use React hooks, the browser `fetch` API, `AbortController`, `Intl` formatting, and an inline SVG timeline. Do not add a server-state, component, or chart library in Milestone 3.
- **Reason:** One dashboard page does not yet justify those dependencies. Typed API functions and focused hooks already provide clear request boundaries, cancellation, and testable component inputs. Libraries can be added later if repeated complexity creates a concrete need.

## ADR-024: Keep financial calculations authoritative in Core

- **Status:** Accepted
- **Decision:** Render analytics returned by the API and do not recalculate monetary totals or win rates in the browser. JavaScript numbers are limited to display formatting and SVG coordinates.
- **Reason:** Core uses `decimal`, while JavaScript uses binary floating-point `number`. Keeping calculations on the backend preserves the tested financial rules and avoids two implementations drifting apart.

## ADR-025: Treat demo configuration as initialization, not live state

- **Status:** Accepted
- **Decision:** Bind and validate `demo-environment.json` into strongly typed options, convert it to Core seed definitions, and add only profiles/accounts whose normalized keys do not exist. Never overwrite an existing persisted record from configuration.
- **Reason:** Configuration provides reproducible fictional starting data, while SQLite remains authoritative after initialization. This allows later user actions and balance changes to survive restarts, migration commands, and edits to default values.

## ADR-026: Model fictional profiles without authentication

- **Status:** Accepted
- **Decision:** Name the entities `DemoProfile` and `DemoAccount` and store no password, token, email, brokerage identifier, or other identity credential. Persist total and reserved balances and calculate available balance.
- **Reason:** The records represent stable actors for repeatable learning scenarios, not real users. Explicit naming and the absence of authentication fields prevent the configuration feature from implying a production identity system or broker connection.

## ADR-027: Model reservation completion as explicit state transitions

- **Status:** Accepted
- **Decision:** Create reservations as `Active` and permit exactly one transition to `Released` or `Consumed`. Release restores available balance; consume decreases both total and reserved balance. Do not expose generic update or delete operations.
- **Reason:** The API expresses business meaning instead of database CRUD. Terminal records preserve useful history, and entity methods keep account and reservation invariants together for focused debugging and tests.

## ADR-028: Separate sequential correctness from concurrency hardening

- **Status:** Accepted
- **Decision:** Milestone 5A persists each sequential account/reservation transition in one EF Core `SaveChanges` operation. Explicitly label it as not concurrency-safe; add the keyed lock, explicit transaction boundary, and durable idempotency together in Milestone 5B.
- **Reason:** The staged implementation gives a runnable baseline and makes the later race condition observable. Claiming concurrency safety before coordination and idempotency exist would teach the wrong guarantee.

## ADR-029: Persist both successful and rejected idempotency outcomes

- **Status:** Accepted
- **Decision:** Store successful creation and insufficient-funds rejection outcomes using a unique `(DemoAccountId, Key)` constraint. Treat keys as case-sensitive opaque values. Replay only when the requested amount matches; return conflict for same-key/different-amount reuse.
- **Reason:** A retry must not produce a different decision merely because balance changed later. Including the request amount prevents one key from accidentally representing two different operations, while the database constraint remains a durable backstop.
