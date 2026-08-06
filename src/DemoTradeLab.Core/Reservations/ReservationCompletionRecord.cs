namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationCompletionRecord
{
    public const int MaximumKeyLength = 100;

    private ReservationCompletionRecord(
        Guid id,
        Guid demoAccountId,
        Guid reservationId,
        string key,
        ReservationCompletionOperation operation,
        DateTimeOffset completedAtUtc)
    {
        Id = id;
        DemoAccountId = demoAccountId;
        ReservationId = reservationId;
        Key = key;
        Operation = operation;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid DemoAccountId { get; private set; }

    public Guid ReservationId { get; private set; }

    public string Key { get; private set; }

    public ReservationCompletionOperation Operation { get; private set; }

    public DateTimeOffset CompletedAtUtc { get; private set; }

    internal static ReservationCompletionRecord Create(
        Guid accountId,
        Guid reservationId,
        string key,
        ReservationCompletionOperation operation,
        DateTimeOffset completedAtUtc) =>
        new(
            Guid.NewGuid(),
            accountId,
            reservationId,
            key,
            operation,
            completedAtUtc);
}
