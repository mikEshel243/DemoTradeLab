using DemoTradeLab.Core.Orders;

namespace DemoTradeLab.Api.Contracts.Orders;

public sealed record OrderResponse(
    Guid Id,
    Guid DemoAccountId,
    Guid ReservationId,
    decimal Amount,
    string Currency,
    OrderStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
