using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Core.Reservations;

public sealed class DemoReservation
{
    private DemoReservation(
        Guid id,
        Guid demoAccountId,
        decimal amount,
        string currency,
        ReservationStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        Id = id;
        DemoAccountId = demoAccountId;
        Amount = amount;
        Currency = currency;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CompletedAtUtc = completedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid DemoAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static ReservationOperationResult Create(
        DemoAccount account,
        decimal amount,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(account);

        var errors = ValidateCreation(amount, createdAtUtc);

        if (errors.Count > 0)
        {
            return ReservationOperationResult.Failure(errors);
        }

        var balanceError = account.Reserve(amount);

        if (balanceError is not null)
        {
            return ReservationOperationResult.Failure(balanceError);
        }

        return ReservationOperationResult.Success(new DemoReservation(
            Guid.NewGuid(),
            account.Id,
            amount,
            account.Currency,
            ReservationStatus.Active,
            createdAtUtc,
            null));
    }

    public ReservationOperationResult Release(
        DemoAccount account,
        DateTimeOffset releasedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(account);

        var error = ValidateCompletion(account, releasedAtUtc);

        if (error is not null)
        {
            return ReservationOperationResult.Failure(error);
        }

        var balanceError = account.Release(Amount);

        if (balanceError is not null)
        {
            return ReservationOperationResult.Failure(balanceError);
        }

        Status = ReservationStatus.Released;
        CompletedAtUtc = releasedAtUtc;

        return ReservationOperationResult.Success(this);
    }

    public ReservationOperationResult Consume(
        DemoAccount account,
        DateTimeOffset consumedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(account);

        var error = ValidateCompletion(account, consumedAtUtc);

        if (error is not null)
        {
            return ReservationOperationResult.Failure(error);
        }

        var balanceError = account.Consume(Amount);

        if (balanceError is not null)
        {
            return ReservationOperationResult.Failure(balanceError);
        }

        Status = ReservationStatus.Consumed;
        CompletedAtUtc = consumedAtUtc;

        return ReservationOperationResult.Success(this);
    }

    private static List<ReservationError> ValidateCreation(
        decimal amount,
        DateTimeOffset createdAtUtc)
    {
        var errors = new List<ReservationError>();

        if (amount <= 0m)
        {
            errors.Add(new ReservationError(
                nameof(Amount),
                ReservationErrorCode.InvalidAmount,
                "Reservation amount must be greater than zero."));
        }

        if (!IsUtc(createdAtUtc))
        {
            errors.Add(new ReservationError(
                nameof(CreatedAtUtc),
                ReservationErrorCode.TimestampMustBeUtc,
                "Reservation creation timestamp must use the UTC offset."));
        }

        return errors;
    }

    private ReservationError? ValidateCompletion(
        DemoAccount account,
        DateTimeOffset completedAtUtc)
    {
        if (Status != ReservationStatus.Active)
        {
            return new ReservationError(
                nameof(Status),
                ReservationErrorCode.ReservationNotActive,
                $"Only an active reservation can be completed; current status is {Status}.");
        }

        if (account.Id != DemoAccountId)
        {
            return new ReservationError(
                nameof(DemoAccountId),
                ReservationErrorCode.AccountMismatch,
                "The reservation does not belong to the supplied demo account.");
        }

        if (!IsUtc(completedAtUtc))
        {
            return new ReservationError(
                nameof(CompletedAtUtc),
                ReservationErrorCode.TimestampMustBeUtc,
                "Reservation completion timestamp must use the UTC offset.");
        }

        if (completedAtUtc < CreatedAtUtc)
        {
            return new ReservationError(
                nameof(CompletedAtUtc),
                ReservationErrorCode.TimestampBeforeCreation,
                "Reservation completion timestamp cannot be earlier than creation.");
        }

        return null;
    }

    private static bool IsUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero;
}
