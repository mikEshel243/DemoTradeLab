using DemoTradeLab.Core.Orders;

namespace DemoTradeLab.Api.Contracts.Orders;

public sealed record OrderEventResponse(
    Guid Id,
    Guid OrderId,
    OrderEventType EventType,
    DateTimeOffset OccurredAtUtc);
