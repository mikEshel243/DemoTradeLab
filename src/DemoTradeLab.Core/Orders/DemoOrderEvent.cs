namespace DemoTradeLab.Core.Orders;

public sealed class DemoOrderEvent
{
    private DemoOrderEvent(
        Guid id,
        Guid orderId,
        OrderEventType eventType,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        OrderId = orderId;
        EventType = eventType;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public OrderEventType EventType { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    internal static DemoOrderEvent Create(
        Guid orderId,
        OrderEventType eventType,
        DateTimeOffset occurredAtUtc) =>
        new(Guid.NewGuid(), orderId, eventType, occurredAtUtc);
}
