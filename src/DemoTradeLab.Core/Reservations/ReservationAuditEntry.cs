namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationAuditEntry
{
    private ReservationAuditEntry(
        Guid id,
        Guid demoAccountId,
        Guid? reservationId,
        ReservationAuditEventType eventType,
        decimal amount,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        DemoAccountId = demoAccountId;
        ReservationId = reservationId;
        EventType = eventType;
        Amount = amount;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid DemoAccountId { get; private set; }

    public Guid? ReservationId { get; private set; }

    public ReservationAuditEventType EventType { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    internal static ReservationAuditEntry Create(
        Guid accountId,
        Guid? reservationId,
        ReservationAuditEventType eventType,
        decimal amount,
        DateTimeOffset occurredAtUtc) =>
        new(
            Guid.NewGuid(),
            accountId,
            reservationId,
            eventType,
            amount,
            occurredAtUtc);
}
