# Testing and Debugging Guide

This is the main operational guide for verifying DemoTradeLab and learning its backend flows. All tests use fictional data. Integration tests create isolated temporary SQLite databases and delete them afterward.

## 1. Verify the complete repository

Open a PowerShell terminal at `C:\Projects\DemoTradeLab` and run:

```powershell
dotnet tool restore
dotnet restore DemoTradeLab.sln
dotnet format DemoTradeLab.sln --verify-no-changes
dotnet build DemoTradeLab.sln --no-restore
dotnet test DemoTradeLab.sln --no-build --no-restore

cd web/demotrade-lab-web
npm install
npm run lint
npm run build
```

A successful backend run prints separate passing totals for `DemoTradeLab.UnitTests` and `DemoTradeLab.IntegrationTests`. A successful frontend build creates ignored files under `web/demotrade-lab-web/dist`.

Verify the EF model and dependency advisories from the repository root:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src/DemoTradeLab.Infrastructure `
  --startup-project src/DemoTradeLab.Api

dotnet list DemoTradeLab.sln package --vulnerable --include-transitive
```

Expected results are `No changes have been made to the model since the last migration` and no vulnerable packages.

## 2. Find tests in VS Code

1. Open the repository root, not an individual project folder.
2. Open the Testing view from the laboratory-flask icon in the left Activity Bar.
3. Expand `DemoTradeLab.UnitTests` or `DemoTradeLab.IntegrationTests`.
4. Select a test and choose Run or Debug.
5. You can also open a test `.cs` file and use the `Run Test` or `Debug Test` CodeLens above `[Fact]`.

If tests do not appear, build the solution once and reload the VS Code window. The C# tooling discovers xUnit tests from the two test projects in the solution.

## 3. Run one test or one category

List every discovered test:

```powershell
dotnet test DemoTradeLab.sln --list-tests
```

Run by partial fully qualified name:

```powershell
dotnet test DemoTradeLab.sln `
  --filter "FullyQualifiedName~ConcurrentReservations_OfEightyAgainstOneHundred"
```

Run a whole class:

```powershell
dotnet test DemoTradeLab.sln `
  --filter "FullyQualifiedName~OrdersControllerTests"
```

Add detailed console output when diagnosing a failing test:

```powershell
dotnet test DemoTradeLab.sln `
  --filter "FullyQualifiedName~Complete_WhenDatabaseWriteFails" `
  --logger "console;verbosity=detailed"
```

## 4. Test catalog by backend concept

| Concept | File | What to debug |
| --- | --- | --- |
| Domain validation | `UnitTests/Trades/TradeTests.cs` | factories, normalization, validation results, immutable identity |
| Query use case | `UnitTests/Trades/TradeServiceQueryTests.cs` | filtering, sorting, service/repository boundary |
| Financial analytics | `UnitTests/Analytics/TradeAnalyticsCalculatorTests.cs` | deterministic decimal calculations and currency separation |
| Demo configuration | `IntegrationTests/Configuration/DemoEnvironmentOptionsTests.cs` | strongly typed options and startup validation |
| EF persistence | `IntegrationTests/Persistence/TradePersistenceTests.cs` | migration, mapping, save, detach, reload |
| Migration-aware seeding | `IntegrationTests/Persistence/DemoProfileSeedingTests.cs` | defaults versus persisted state |
| CRUD HTTP flow | `IntegrationTests/TradesControllerTests.cs` | routing, controller, service, EF Core, status codes |
| Reservation domain | `UnitTests/Reservations/DemoReservationTests.cs` | reserve, release, consume, balance invariants |
| Atomic orchestration | `UnitTests/Reservations/ReservationServiceTests.cs` | lock, transaction, idempotency, audit, commit counts |
| Reservation HTTP flow | `IntegrationTests/ReservationsControllerTests.cs` | 400/404/409, replay headers, persistent balances |
| Production lock | `IntegrationTests/LocalAccountLockManagerTests.cs` | same-key waiting, different-key independence, cancellation, exception release |
| Controlled concurrency | `IntegrationTests/ReservationConcurrencyTests.cs` | deterministic overlap and exact final database state |
| Order domain | `UnitTests/Orders/DemoOrderTests.cs` | state machine and compensation rules |
| Recovery HTTP flow | `IntegrationTests/OrdersControllerTests.cs` | fail, reconcile, compensate, technical rollback, retry |

## 5. Recommended debugger journey

Follow these tests in order. Each adds one backend layer.

### A. Pure domain

Debug `DemoReservationTests.Create_WithAvailableFunds_ReservesBalanceAndCreatesActiveReservation`.

Breakpoints:

- `DemoReservation.Create`
- `DemoAccount.Reserve`

No HTTP or database exists in this test. Watch `TotalBalance`, `ReservedBalance`, and `AvailableBalance` before and after the domain call.

### B. Application orchestration

Debug `ReservationServiceTests.CreateAsync_WithExistingAccount_PersistsAtomicOperationRecords`.

Breakpoints:

- `ReservationService.CreateAsync`
- the in-memory repository methods inside the test

Watch the exact order: lock, transaction, idempotency lookup, account load, domain call, three added records, save, commit, lease disposal.

### C. Full HTTP and SQLite

Debug `ReservationsControllerTests.CreateReadListRelease_FullLifecyclePersistsExpectedState`.

Breakpoints:

- `ReservationsController.CreateAsync`
- `ReservationService.CreateAsync`
- `EfReservationRepository.GetAccountForUpdateAsync`
- `DemoReservation.Create`
- `EfReservationRepository.SaveChangesAsync`
- `EfReservationTransaction.CommitAsync`

This test uses `WebApplicationFactory`, real HTTP serialization, dependency injection, EF Core, migrations, and a temporary SQLite file.

### D. Deterministic concurrency

Debug `ReservationConcurrencyTests.ConcurrentReservations_OfEightyAgainstOneHundred_ProduceOneSuccess`.

Watch request A pause while holding the controlled lock, request B attempt it, A commit, and B then read available balance `20`. Do not replace the gates with arbitrary delays.

### E. Recovery and compensation

Debug `OrdersControllerTests.FailThenCompensate_ReleasesFundsAndClearsRecoveryWork`.

Watch the order remain `Failed` while funds stay reserved. Call reconciliation, then continue into compensation and watch the reservation become `Released`.

### F. Technical rollback

Debug `OrdersControllerTests.Complete_WhenDatabaseWriteFails_RollsBackAndCanBeRetried`.

The temporary SQLite trigger forces `SaveChangesAsync` to throw. Inspect the domain objects before the exception, then inspect the database state loaded by the next request. The transaction prevents partial persistence.

## 6. HTTP status cases worth learning

| Status | Meaning in this project | Example |
| --- | --- | --- |
| 200 | Read or idempotent/replayed state transition succeeded | retry completed order |
| 201 | New resource created | trade, reservation, or order |
| 204 | Successful delete without a body | delete trade |
| 400 | Request shape or domain validation failed | missing idempotency key, invalid amount |
| 404 | Route target does not exist | missing account, trade, reservation, or order |
| 409 | Valid request conflicts with current business state | insufficient funds, contradictory transition, idempotency mismatch |
| 500 | Unexpected technical failure | simulated SQLite write failure |

Expected business outcomes use result objects and Problem Details. Unexpected infrastructure failures throw, are rolled back, and reach the exception handler as HTTP 500.

## 7. Run the application manually

Apply migrations explicitly:

```powershell
dotnet ef database update `
  --project src/DemoTradeLab.Infrastructure `
  --startup-project src/DemoTradeLab.Api
```

Start the API:

```powershell
dotnet run --project src/DemoTradeLab.Api --launch-profile http
```

Use `src/DemoTradeLab.Api/DemoTradeLab.Api.http` in VS Code. Run `GET /api/demo-profiles`, copy a fictional account ID into the variables, then execute reservation and order requests in sequence.

Start the dashboard in a second terminal:

```powershell
cd web/demotrade-lab-web
npm install
npm run dev
```

Open `http://localhost:5173`.

## 8. Manual end-to-end checklist

1. `GET /api/health` returns 200.
2. `GET /api/demo-profiles` returns fictional profiles and account balances.
3. Create a reservation with a unique `Idempotency-Key`.
4. Repeat it with the same key and confirm `Idempotency-Replayed: true`.
5. Create an order from the reservation.
6. Mark the order failed and confirm funds remain reserved.
7. Call reconciliation and confirm one failed order awaits compensation.
8. Compensate the order and confirm available balance is restored.
9. Read order events and confirm `created`, `failed`, `compensated`.
10. Run the full automated test suite again.

## 9. Coverage collection

The test projects already reference the Coverlet collector. Generate a local Cobertura report without committing test artifacts:

```powershell
dotnet test DemoTradeLab.sln `
  --collect:"XPlat Code Coverage" `
  --results-directory "$env:TEMP\DemoTradeLab-TestResults"
```

Coverage is a navigation aid, not proof of correctness. The scenario assertions, state invariants, database counts, HTTP contracts, concurrency gates, and rollback tests are more important than maximizing one percentage.

## 10. Common troubleshooting

- `no such table`: apply the committed EF migrations.
- Port already in use: stop the existing API/frontend process or use a different explicit port.
- Tests are not discovered: build the solution and reload VS Code.
- A focused test filter returns no tests: run `--list-tests` and copy part of the exact name.
- Local database has old experimental state: point the connection string to a new local database rather than deleting unknown files.
- HTTP 400 on reservation completion: supply a unique `Idempotency-Key` header.
