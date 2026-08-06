# Roadmap

## Milestone 0 - Repository foundation

Status: Complete

- [x] Validate .NET and Git tooling
- [x] Create and reference Api, Core, Infrastructure, UnitTests, and IntegrationTests projects
- [x] Configure repository ignores for build output, local databases, uploads, and secrets
- [x] Add repository and architecture documentation
- [x] Add a controller-based health endpoint
- [x] Add an integration test for the health endpoint
- [x] Build and test the backend solution

## Milestone 1 - Trade domain

Status: Complete

- [x] Trade domain model and validation
- [x] Unit tests for trade invariants
- [x] EF Core with SQLite and migrations
- [x] Integration test for migration and persistence mapping
- [x] Fictional seed data
- [x] Trade CRUD endpoints
- [x] Integration tests for CRUD endpoints

## Milestone 2 - Analytics API

Status: Complete

- [x] Dashboard statistics
- [x] Filtering and sorting
- [x] Instrument summaries
- [x] Profit/loss timeline
- [x] Calculation edge-case tests

## Milestone 3 - React dashboard

Status: Complete

- [x] React, TypeScript, and Vite application
- [x] Dashboard summary and currency performance
- [x] Instrument summaries and profit/loss timeline
- [x] Filterable and sortable trade table
- [x] Trade-details view backed by the single-trade endpoint
- [x] Loading, error, and empty states
- [x] Responsive layout and reduced-motion support
- [x] Frontend lint and production build

## Milestone 4 - Configurable demo environment

Status: Complete

- [x] Strongly typed and startup-validated fictional profile/account configuration
- [x] `DemoProfile` and `DemoAccount` domain models with normalized unique keys
- [x] Persisted total and reserved balances with calculated available balance
- [x] EF Core mappings, uniqueness constraints, migration, and focused repository
- [x] Migration-aware initialization that adds missing configured records without overwriting persisted state
- [x] Read-only `GET /api/demo-profiles` endpoint with explicit response DTOs
- [x] Domain, configuration, persistence, and endpoint tests

## Milestone 5 - Reliability simulator

Status: Planned

This milestone is a separate educational module. Its primary concurrency example will use locking; optimistic concurrency may be compared as an alternative but will not silently replace the lock-based lesson.

### 5A - Reservation foundation and account state transitions

- Extend `DemoAccount` with explicit reserve, release, and consume state transitions
- Enforce `0 <= ReservedBalance <= TotalBalance` for every state transition inside the domain
- Add the minimum useful reservation lifecycle: `Active`, `Released`, and `Consumed`
- Add explicit result types for success, insufficient funds, account not found, and validation rejection
- Add focused reservation persistence, EF Core mappings, and a migration; reuse the Milestone 4 demo accounts

### 5B - Atomic lock-based reservation

- Add `IAccountLockManager` and a keyed local implementation using one asynchronous lock per account
- Acquire the account lock before reading authoritative state
- Acquire only one account lock in the initial operation; require a stable global account-ID order before any future operation may acquire multiple locks
- Inside the minimum critical section, begin a transaction, check durable idempotency, load the account, validate available funds, create the reservation and audit record, save, and commit
- Add a durable idempotency key with a database uniqueness constraint
- Expose `POST /api/accounts/{accountId}/reservations` with an `Idempotency-Key` header
- Map expected business outcomes to response DTOs and Problem Details without exception-based business control flow
- Record structured logs without sensitive data

### 5C - Deterministic concurrency verification

- Prove that concurrent reservations of `80` and `80` against a balance of `100` produce exactly one success and one insufficient-funds rejection
- Use a barrier, gate, or controlled test hook so overlap is deterministic rather than timing-dependent
- Prove that different account keys do not share one global application lock
- Prove that a duplicate idempotency key returns the original result and reserves funds once
- Prove that insufficient funds do not modify state
- Prove that cancellation and exceptions always release the lock
- Prove the single-lock rule and document ordered acquisition for any future multi-account operation
- Assert account balance invariants after every relevant test

### 5D - Reservation completion and recovery

- Add idempotent consume and release operations
- Demonstrate release or compensation after a later operation fails
- Add an explicit order state machine and distinguish technical failure from business rejection
- Add retry-safety, audit-history, reconciliation, and simulated failure scenarios

### SQLite and multi-instance boundary

- The initial local lock coordinates only requests handled by one application process
- SQLite does not provide SQL Server/PostgreSQL-style row-level `SELECT FOR UPDATE` semantics
- SQLite may still serialize database writes even when different account application locks are independent
- Do not claim multi-instance safety until a provider-specific database-locking strategy is implemented and tested
- A future multi-instance exercise may use a server database with genuine row locking, but no new external infrastructure is part of the current MVP

Readiness: the authoritative demo-account state and persistence mapping now exist. Begin 5A by defining protected balance transitions and the reservation lifecycle; do not start with the lock manager alone because transaction ownership and reservation/idempotency models are still prerequisites.

## Milestone 6 - Final polish

Status: Planned

- Documentation and screenshots
- Architecture diagram
- Test-coverage improvements
- Interview demonstration script
