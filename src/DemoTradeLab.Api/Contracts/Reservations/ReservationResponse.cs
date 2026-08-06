using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.Api.Contracts.Reservations;

public sealed record ReservationResponse(
    Guid Id,
    Guid DemoAccountId,
    decimal Amount,
    string Currency,
    ReservationStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc);
