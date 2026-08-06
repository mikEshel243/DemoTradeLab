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

Debug `CreateAsync_WithExistingAccount_OrchestratesDomainAndPersistence` and follow:

```text
ReservationService.CreateAsync
    -> repository.GetAccountForUpdateAsync
    -> DemoReservation.Create
    -> repository.Add
    -> repository.SaveChangesAsync
```

The in-memory repository is a test double. It isolates orchestration from EF Core and proves that rejected operations do not call save.

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

These show HTTP 400 validation, HTTP 404 missing resources, HTTP 409 business conflicts, and the rule that rejected requests must not mutate data.

## Running a focused test

From VS Code, use the `Debug Test` action displayed above an xUnit test. You can also filter from the terminal before debugging the same test in the Test Explorer:

```powershell
dotnet test DemoTradeLab.sln --filter "FullyQualifiedName~CreateReadListRelease_FullLifecyclePersistsExpectedState"
```

For the clearest first walkthrough, place breakpoints in the controller, service, `EfReservationRepository.SaveChangesAsync`, and domain entity, then use Step Into to move between layers.

## Current concurrency boundary

Milestone 5A proves sequential correctness only. Do not infer that simultaneous reservation requests are safe. Milestone 5B will deliberately reproduce and then protect that race with a per-account asynchronous lock, explicit transaction boundary, and durable idempotency record. Its tests will use controlled synchronization so the race does not depend on random timing.
