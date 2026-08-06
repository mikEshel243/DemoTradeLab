namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationIdempotencyRecord
{
    public const int MaximumKeyLength = 100;

    private ReservationIdempotencyRecord(
        Guid id,
        Guid demoAccountId,
        string key,
        decimal requestedAmount,
        ReservationIdempotencyOutcome outcome,
        Guid? reservationId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        DemoAccountId = demoAccountId;
        Key = key;
        RequestedAmount = requestedAmount;
        Outcome = outcome;
        ReservationId = reservationId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid DemoAccountId { get; private set; }

    public string Key { get; private set; }

    public decimal RequestedAmount { get; private set; }

    public ReservationIdempotencyOutcome Outcome { get; private set; }

    public Guid? ReservationId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static ReservationIdempotencyRecord Created(
        Guid accountId,
        string key,
        decimal amount,
        Guid reservationId,
        DateTimeOffset createdAtUtc) =>
        new(
            Guid.NewGuid(),
            accountId,
            key,
            amount,
            ReservationIdempotencyOutcome.Created,
            reservationId,
            createdAtUtc);

    internal static ReservationIdempotencyRecord InsufficientFunds(
        Guid accountId,
        string key,
        decimal amount,
        DateTimeOffset createdAtUtc) =>
        new(
            Guid.NewGuid(),
            accountId,
            key,
            amount,
            ReservationIdempotencyOutcome.InsufficientFunds,
            null,
            createdAtUtc);
}
