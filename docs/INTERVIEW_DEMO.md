# Interview Demonstration Script

This script presents DemoTradeLab as an educational backend project, not a real trading platform. All data is fictional and the application has no broker connection or trading recommendation functionality.

## Five-minute overview

### 1. Architecture - 45 seconds

Open `docs/ARCHITECTURE_DIAGRAM.md`.

Explain:

- Api owns controllers, DTOs, configuration, and HTTP error mapping.
- Core owns domain rules and application-service orchestration without EF Core or ASP.NET Core references.
- Infrastructure owns SQLite, EF mappings, repositories, transactions, seeding, and the process-local lock.
- The projects form a modular monolith, avoiding premature distributed-system complexity.

### 2. Domain modeling - 45 seconds

Open `DemoReservation.cs`, `DemoAccount.cs`, and `DemoOrder.cs`.

Explain:

- Factories prevent invalid initial entities.
- Expected business rejection is returned as explicit result data.
- `AvailableBalance` is calculated from total minus reserved.
- Reservation and order state machines expose meaningful operations rather than generic CRUD updates.

### 3. Atomic reservation - 60 seconds

Open `ReservationService.CreateAsync`.

Walk through:

1. Validate the idempotency key.
2. Acquire one account lock asynchronously.
3. Begin the transaction.
4. Replay any durable previous outcome.
5. Load authoritative account state.
6. Apply the domain invariant.
7. Save reservation, idempotency, and audit records.
8. Commit and release.

State the boundary: the lock coordinates one application process. It is not a distributed lock or SQLite row lock.

### 4. Deterministic concurrency test - 60 seconds

Open `ReservationConcurrencyTests.ConcurrentReservations_OfEightyAgainstOneHundred_ProduceOneSuccess`.

Explain why gates are used instead of sleeps. Show the final assertions: one success, one conflict, one reservation, two durable outcomes, total 100, reserved 80, available 20.

### 5. Failure and recovery - 60 seconds

Open `OrdersControllerTests.Complete_WhenDatabaseWriteFails_RollsBackAndCanBeRetried`.

Explain:

- A temporary SQLite trigger simulates a technical persistence failure.
- Domain objects change inside the failed request, but the transaction does not commit.
- The next request reloads the original pending state.
- Removing the failure and retrying succeeds.
- This differs from a 409 business rejection, which is expected control flow.

### 6. Verification - 30 seconds

Run:

```powershell
dotnet test DemoTradeLab.sln
```

Mention frontend lint/build, migration-model checking, and vulnerability auditing.

## Ten-minute live demonstration

1. Run `GET /api/demo-profiles` and choose a fictional account.
2. Create a reservation with `Idempotency-Key: interview-create-1`.
3. Repeat the request and show `Idempotency-Replayed: true`.
4. Create an order from that reservation.
5. Mark it failed.
6. Show reconciliation reporting one failed order and reserved funds.
7. Compensate it.
8. Show reconciliation healthy and order-event history.
9. Run the focused deterministic concurrency test.
10. Finish with the architecture boundary and what would change for multi-instance deployment.

## Likely interview questions

### Why not use exceptions for insufficient funds?

Insufficient funds is an expected business outcome. A result object keeps it explicit, testable, and maps cleanly to HTTP 409. Exceptions remain for unexpected technical failures.

### Why both a lock and a database transaction?

The local lock serializes same-account operations inside one process. The transaction guarantees all database writes commit or roll back together. They solve different problems.

### Why persist idempotency?

An in-memory key disappears after restart. A durable record in the same transaction prevents a balance change without its retry outcome and allows consistent replay later.

### Why is the SQLite solution not multi-instance safe?

Each process owns a different semaphore. SQLite does not provide the row-locking semantics assumed by common server-database pessimistic-lock designs. Cross-instance correctness requires a provider-specific or distributed coordination strategy.

### Why separate failure and compensation?

Recovery can itself be delayed or fail. Persisting `Failed` makes unfinished recovery visible to reconciliation; a later explicit compensation command is independently retryable.

### What would you improve for production scale?

Authentication/authorization, a server database, provider-specific concurrency control, pagination, observability, migration deployment policy, distributed tracing, retention policies, and cross-instance tests. None should be implied by this intentionally local educational project.
