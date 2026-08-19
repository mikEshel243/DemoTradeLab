# API Reference

This reference describes the currently implemented HTTP API. Every profile, account, trade, reservation, and order in DemoTradeLab is fictional or manually entered.

The default local base URL is `http://localhost:5122`. In Development, the generated OpenAPI document is available at `/openapi/v1.json`, and `src/DemoTradeLab.Api/DemoTradeLab.Api.http` contains executable request examples.

## Health

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/health` | Returns application health status and a UTC check timestamp |

## Trades

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/trades` | Lists completed trades with optional filters and sorting |
| `GET` | `/api/trades/{id}` | Returns one trade or HTTP 404 Problem Details |
| `POST` | `/api/trades` | Creates a manually entered trade and returns HTTP 201 |
| `PUT` | `/api/trades/{id}` | Replaces editable fields on an existing trade |
| `DELETE` | `/api/trades/{id}` | Deletes a trade and returns HTTP 204 |

Create and update requests accept string enum values such as `buy` and `sell`. API-created trades always receive the `manual` source; clients cannot claim that a new record came from sample or imported data.

### Trade list parameters

| Parameter | Values or meaning |
| --- | --- |
| `instrument` | Exact instrument match, case-insensitive |
| `currency` | Three-letter currency code, case-insensitive |
| `direction` | `buy` or `sell` |
| `source` | `manual`, `sample`, or `imported` |
| `outcome` | `profitable`, `losing`, or `breakEven` |
| `closedFromUtc` | Inclusive UTC closing-time lower bound |
| `closedToUtc` | Inclusive UTC closing-time upper bound |
| `sortBy` | `closedAtUtc`, `openedAtUtc`, `instrument`, `realizedProfitLoss`, or `duration` |
| `sortDirection` | `ascending` or `descending` |

The default order is newest closing time first. For example:

```http
GET /api/trades?instrument=EUR%2FUSD&outcome=profitable&sortBy=realizedProfitLoss&sortDirection=descending
```

## Analytics

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/analytics/dashboard` | Counts, win rate, most active instrument, average duration, and currency performance |
| `GET` | `/api/analytics/instruments` | Statistics grouped by instrument and currency |
| `GET` | `/api/analytics/profit-loss-timeline` | Chronological and cumulative realized profit/loss points per currency |

Win rate is the number of profitable trades divided by all completed trades, including break-even trades in the denominator. Monetary values are never summed across different currencies. Best trade, worst trade, total realized profit/loss, and timeline totals are therefore currency-separated.

Analytics use `RealizedProfitLoss` as stored. Optional fees and financing costs are exposed separately and are not subtracted a second time.

## Fictional Demo Profiles and Accounts

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/demo-profiles` | Lists persisted fictional profiles, accounts, and balances |

`src/DemoTradeLab.Api/demo-environment.json` supplies initialization data, not authenticated identities. The configured records have no passwords, credentials, tokens, email addresses, or broker connections.

Each account stores total and reserved balance. Available balance is calculated as `totalBalance - reservedBalance` rather than persisted as a third value. Applying migrations adds missing configured profiles and accounts without resetting balances already stored in SQLite.

## Reservations

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/demo-accounts/{accountId}/reservations` | Lists reservations for an existing demo account |
| `GET` | `/api/demo-accounts/{accountId}/reservations/{reservationId}` | Returns one reservation |
| `POST` | `/api/demo-accounts/{accountId}/reservations` | Reserves available funds and returns HTTP 201 |
| `POST` | `/api/demo-accounts/{accountId}/reservations/{reservationId}/release` | Releases reserved funds back to available balance |
| `POST` | `/api/demo-accounts/{accountId}/reservations/{reservationId}/consume` | Deducts consumed funds from total and reserved balance |

Create, release, and consume requests require an `Idempotency-Key` header. Keys are case-sensitive, trimmed, and limited to 100 characters.

- Retrying creation with the same account, key, and amount replays the stored outcome and adds `Idempotency-Replayed: true`.
- Both successful creation and insufficient-funds rejection are durable, so replay works after an application restart.
- Reusing a creation key with a different amount returns HTTP 409.
- Retrying the same release or consume operation replays success without a second balance change or audit event.
- Reusing a completion key for a different reservation or operation returns HTTP 409.

An amount greater than available balance returns HTTP 409 without changing account state. Invalid request data returns HTTP 400, missing resources return HTTP 404, and contradictory state transitions return HTTP 409.

Reservations are not edited or deleted. Business operations move them from `Active` to exactly one terminal state: `Released` or `Consumed`.

## Orders, Recovery, and Reconciliation

An order is created from one active reservation and follows one supported path:

```text
Pending -> Completed
Pending -> Failed -> Compensated
```

| Method | Route | Result |
| --- | --- | --- |
| `GET` | `/api/demo-accounts/{accountId}/orders` | Lists account orders |
| `GET` | `/api/demo-accounts/{accountId}/orders/{orderId}` | Returns one order |
| `POST` | `/api/demo-accounts/{accountId}/orders` | Creates an order for an active reservation or returns the existing order |
| `POST` | `/api/demo-accounts/{accountId}/orders/{orderId}/complete` | Consumes the reservation and completes the order |
| `POST` | `/api/demo-accounts/{accountId}/orders/{orderId}/fail` | Records a simulated later failure |
| `POST` | `/api/demo-accounts/{accountId}/orders/{orderId}/compensate` | Releases funds for a failed order |
| `GET` | `/api/demo-accounts/{accountId}/orders/{orderId}/events` | Returns durable order history |
| `GET` | `/api/demo-accounts/{accountId}/orders/reconciliation` | Compares reserved balance with active reservations and reports failed orders |

Completing an order consumes reserved funds. Marking an order failed deliberately leaves its reservation active so unfinished recovery remains visible. Compensation later releases the reservation. Repeating a transition already at its target is a successful no-op; a contradictory transition returns HTTP 409.

Reconciliation reports whether persisted reserved balance equals the sum of active reservations and counts failed orders awaiting compensation. It does not automatically change either value.

## Error Responses

- HTTP 400: request binding, annotation validation, or domain validation failed.
- HTTP 404: the requested account, reservation, order, or trade does not exist.
- HTTP 409: a valid request conflicts with current business state, available funds, or prior idempotency input.
- HTTP 500: an unexpected technical failure reached the global exception handler.

Expected failures use validation responses or Problem Details. Unexpected persistence exceptions are not converted into business results; explicit transactions protect their database changes from partial commit.
