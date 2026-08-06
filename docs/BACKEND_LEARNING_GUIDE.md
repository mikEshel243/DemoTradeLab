# Backend Learning Guide

This guide identifies focused tests that can be run under the debugger to observe each backend layer. All data is fictional and local.

## Reservation test layers

### 1. Domain rules without HTTP or a database

Open `tests/DemoTradeLab.UnitTests/Reservations/DemoReservationTests.cs` and debug one test at a time.

Recommended starting tests:

- `Create_WithAvailableFunds_ReservesBalanceAndCreatesActiveReservation`
- `Create_WithInsufficientFunds_ReturnsRejectionWithoutChangingBalance`
- `Release_ActiveReservation_RestoresAvailableBalance`
- `Consume_ActiveReservation_ReducesTotalAndReservedBalances`
- `Release_CompletedReservation_ReturnsRejectionWithoutChangingBalance`

Useful breakpoints:

- `DemoReservation.Create`
- `DemoAccount.Reserve`
- `DemoReservation.Release` or `DemoReservation.Consume`
- `DemoAccount.Release` or `DemoAccount.Consume`

This layer teaches entity state, invariants, expected result objects, and the difference between a business rejection and an exception.

## 2. Application-service orchestration

Open `tests/DemoTradeLab.UnitTests/Reservations/ReservationServiceTests.cs`.

Debug `CreateAsync_WithExistingAccount_PersistsAtomicOperationRecords` and follow:

```text
ReservationService.CreateAsync
    -> lockManager.AcquireAsync
    -> repository.BeginTransactionAsync
    -> repository.GetIdempotencyRecordAsync
    -> repository.GetAccountForUpdateAsync
    -> DemoReservation.Create
    -> add reservation, idempotency, and audit records
    -> repository.SaveChangesAsync
    -> transaction.CommitAsync
    -> release lock
```

The in-memory repository and immediate lock are test doubles. They isolate orchestration from EF Core and make save/commit/lock counts visible. Next debug `CreateAsync_WithSameKey_ReplaysOriginalSuccessWithoutSavingAgain` and `CreateAsync_WithInsufficientFunds_PersistsAndReplaysRejection` to compare the first operation with a replay.

## 3. Full HTTP-to-SQLite flow

Open `tests/DemoTradeLab.IntegrationTests/ReservationsControllerTests.cs`.

Debug `CreateReadListRelease_FullLifecyclePersistsExpectedState` to follow:

```text
HTTP POST
    -> ASP.NET Core routing and DTO validation
    -> ReservationsController
    -> ReservationService
    -> EfReservationRepository
    -> DemoReservation and DemoAccount
    -> EF Core SaveChanges
    -> SQLite
    -> response DTO and HTTP 201
```

Then the same test performs GET, list, release, and balance verification through real HTTP requests and a temporary SQLite database.

Other recommended integration tests:

- `Consume_ActiveReservation_ReducesPersistedTotalBalance`
- `Create_WithInvalidAmount_ReturnsAutomaticValidationProblem`
- `Create_WithInsufficientFunds_ReturnsConflictWithoutChangingState`
- `Create_ForMissingAccount_ReturnsNotFound`
- `Consume_ReleasedReservation_ReturnsConflictWithoutSecondBalanceChange`
- `Create_WithSameIdempotencyKey_ReplaysPersistedReservationOnce`
- `Create_WithReusedKeyAndDifferentAmount_ReturnsConflict`

These show HTTP 400 validation, HTTP 404 missing resources, HTTP 409 business conflicts, and the rule that rejected requests must not mutate data.

## Running a focused test

From VS Code, use the `Debug Test` action displayed above an xUnit test. You can also filter from the terminal before debugging the same test in the Test Explorer:

```powershell
dotnet test DemoTradeLab.sln --filter "FullyQualifiedName~CreateReadListRelease_FullLifecyclePersistsExpectedState"
```

For the clearest first walkthrough, place breakpoints in the controller, service, `EfReservationRepository.SaveChangesAsync`, and domain entity, then use Step Into to move between layers.

## Lock-manager behavior

Open `tests/DemoTradeLab.IntegrationTests/LocalAccountLockManagerTests.cs`.

- `AcquireAsync_ForSameAccount_WaitsUntilFirstLeaseIsReleased` shows serialization for one account.
- `AcquireAsync_ForDifferentAccounts_UsesIndependentLocks` shows why a keyed lock permits unrelated accounts to proceed independently.

Useful breakpoints are `LocalAccountLockManager.AcquireAsync`, `SemaphoreSlim.WaitAsync`, and `AccountLockLease.DisposeAsync`.

## Current concurrency boundary

Open `tests/DemoTradeLab.IntegrationTests/ReservationConcurrencyTests.cs` and debug `ConcurrentReservations_OfEightyAgainstOneHundred_ProduceOneSuccess`.

Recommended breakpoint order:

1. `ControlledAccountLockManager.AcquireAsync`
2. `ReservationService.CreateAsync`
3. `EfReservationRepository.BeginTransactionAsync`
4. `DemoReservation.Create`
5. `EfReservationRepository.SaveChangesAsync`
6. `EfReservationTransaction.CommitAsync`

The test deliberately performs this sequence:

```text
start request A
    -> A acquires the controlled account lock and pauses
start request B
    -> B reports that it attempted the same lock and waits
open A's gate
    -> A reserves 80 and commits
    -> A releases the lock
    -> B loads the new available balance of 20
    -> B persists an insufficient-funds outcome
```

Inspect the final assertions for all database effects: one reservation, two idempotency records, two audit entries, total `100`, reserved `80`, and available `20`.

Then debug `ConcurrentDuplicateKey_ReplaysOnePersistedReservation` to see both simultaneous requests return the same reservation ID while only one reservation, idempotency record, and audit entry are stored.

Milestone 5C proves single-process coordination only. A local `SemaphoreSlim` cannot coordinate separate application processes. Multi-instance correctness would require a provider-specific database or distributed coordination strategy and cross-instance tests.

## Idempotent completion flow

Debug `Release_WithSameIdempotencyKey_ReplaysWithoutSecondAuditOrBalanceChange` in `ReservationsControllerTests.cs`.

Follow this order:

```text
first release request
    -> validate completion Idempotency-Key
    -> lock account and begin transaction
    -> no completion record exists
    -> release reserved balance
    -> write completion record and one Released audit event
    -> commit

retry with the same key
    -> load completion record
    -> load already released reservation
    -> return success with Idempotency-Replayed: true
    -> no second balance change or audit event
```

Then debug `Completion_WithReusedKeyForDifferentOperation_ReturnsConflict` to see a release key rejected when reused for consume.

## Order state machine and recovery

Start with the pure domain test `FailThenCompensate_ReleasesReservationInSeparateTransition` in `tests/DemoTradeLab.UnitTests/Orders/DemoOrderTests.cs`.

Important states:

```text
Pending -> Completed
Pending -> Failed -> Compensated
```

Next debug `FailThenCompensate_ReleasesFundsAndClearsRecoveryWork` in `OrdersControllerTests.cs`.

Recommended breakpoints:

1. `OrdersController.MarkFailedAsync`
2. `OrderService.MarkFailedAsync`
3. `DemoOrder.MarkFailed`
4. `OrderService.ReconcileAsync`
5. `DemoOrder.Compensate`
6. `DemoReservation.Release`
7. `EfOrderRepository.SaveChangesAsync`
8. `EfReservationTransaction.CommitAsync`

Observe that `Failed` does not release money. The reconciliation endpoint reports one failed order requiring compensation. Only the later compensation transaction releases the reservation and changes the order to `Compensated`.

## Technical rollback and retry

Debug `Complete_WhenDatabaseWriteFails_RollsBackAndCanBeRetried` in `OrdersControllerTests.cs`.

The test creates a temporary SQLite trigger that rejects the `Completed` event insert. Step through the domain changes and notice that the tracked objects temporarily look completed inside the request. `SaveChangesAsync` then throws, the transaction is disposed without commit, and the HTTP response is 500. A new request reloads `Pending`, active reservation, and reserved balance `80` from SQLite. After removing the trigger, retry succeeds.

Compare this with `Complete_FailedOrder_ReturnsConflictWithoutChangingFunds`:

- HTTP 409 is an expected business rejection returned as a result.
- HTTP 500 is an unexpected technical failure caused by persistence.
- Neither path leaves a partial committed balance/order state.

## Reconciliation

Debug `Reconciliation_WhenPersistedBalanceIsCorrupted_ReportsMismatch`. The test intentionally changes only the test database balance using SQL, then calls the real reconciliation endpoint. The report compares stored `ReservedBalance` with the sum of active reservation amounts and returns `IsBalanceConsistent = false`. It detects but does not automatically repair the mismatch.
