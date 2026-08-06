using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.Core.Orders;

public sealed class DemoOrder
{
    private DemoOrder(
        Guid id,
        Guid demoAccountId,
        Guid reservationId,
        decimal amount,
        string currency,
        OrderStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        DemoAccountId = demoAccountId;
        ReservationId = reservationId;
        Amount = amount;
        Currency = currency;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid DemoAccountId { get; private set; }

    public Guid ReservationId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static OrderOperationResult Create(
        DemoReservation reservation,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        if (reservation.Status != ReservationStatus.Active)
        {
            return OrderOperationResult.Failure(new OrderError(
                nameof(reservation),
                OrderErrorCode.ReservationNotActive,
                "An order can be created only from an active reservation."));
        }

        if (!IsUtc(createdAtUtc))
        {
            return InvalidTimestamp(nameof(createdAtUtc));
        }

        return OrderOperationResult.Success(new DemoOrder(
            Guid.NewGuid(),
            reservation.DemoAccountId,
            reservation.Id,
            reservation.Amount,
            reservation.Currency,
            OrderStatus.Pending,
            createdAtUtc,
            createdAtUtc));
    }

    public OrderOperationResult Complete(
        DemoReservation reservation,
        DemoAccount account,
        DateTimeOffset completedAtUtc)
    {
        if (Status == OrderStatus.Completed)
        {
            return OrderOperationResult.Success(this, isNoOp: true);
        }

        var error = ValidateTransition(
            reservation,
            account,
            completedAtUtc,
            OrderStatus.Pending,
            "Only a pending order can be completed.");

        if (error is not null)
        {
            return OrderOperationResult.Failure(error);
        }

        var reservationResult = reservation.Consume(account, completedAtUtc);

        if (!reservationResult.IsSuccess)
        {
            return FromReservationFailure(reservationResult);
        }

        Status = OrderStatus.Completed;
        UpdatedAtUtc = completedAtUtc;
        return OrderOperationResult.Success(this);
    }

    public OrderOperationResult MarkFailed(DateTimeOffset failedAtUtc)
    {
        if (Status == OrderStatus.Failed)
        {
            return OrderOperationResult.Success(this, isNoOp: true);
        }

        if (Status != OrderStatus.Pending)
        {
            return InvalidState("Only a pending order can be marked as failed.");
        }

        if (!IsValidTransitionTime(failedAtUtc))
        {
            return InvalidTimestamp(nameof(failedAtUtc));
        }

        Status = OrderStatus.Failed;
        UpdatedAtUtc = failedAtUtc;
        return OrderOperationResult.Success(this);
    }

    public OrderOperationResult Compensate(
        DemoReservation reservation,
        DemoAccount account,
        DateTimeOffset compensatedAtUtc)
    {
        if (Status == OrderStatus.Compensated)
        {
            return OrderOperationResult.Success(this, isNoOp: true);
        }

        var error = ValidateTransition(
            reservation,
            account,
            compensatedAtUtc,
            OrderStatus.Failed,
            "Only a failed order can be compensated.");

        if (error is not null)
        {
            return OrderOperationResult.Failure(error);
        }

        var reservationResult = reservation.Release(account, compensatedAtUtc);

        if (!reservationResult.IsSuccess)
        {
            return FromReservationFailure(reservationResult);
        }

        Status = OrderStatus.Compensated;
        UpdatedAtUtc = compensatedAtUtc;
        return OrderOperationResult.Success(this);
    }

    private OrderError? ValidateTransition(
        DemoReservation reservation,
        DemoAccount account,
        DateTimeOffset occurredAtUtc,
        OrderStatus requiredStatus,
        string invalidStateMessage)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentNullException.ThrowIfNull(account);

        if (Status != requiredStatus)
        {
            return new OrderError(
                nameof(Status),
                OrderErrorCode.InvalidState,
                invalidStateMessage);
        }

        if (reservation.Id != ReservationId || account.Id != DemoAccountId)
        {
            return new OrderError(
                nameof(ReservationId),
                OrderErrorCode.AccountMismatch,
                "The order, reservation, and account do not belong to the same workflow.");
        }

        if (!IsValidTransitionTime(occurredAtUtc))
        {
            return new OrderError(
                nameof(occurredAtUtc),
                OrderErrorCode.InvalidTimestamp,
                "Order transition time must be UTC and cannot be earlier than creation.");
        }

        return null;
    }

    private bool IsValidTransitionTime(DateTimeOffset value) =>
        IsUtc(value) && value >= CreatedAtUtc;

    private static OrderOperationResult FromReservationFailure(
        ReservationOperationResult result)
    {
        var error = result.Errors[0];
        return OrderOperationResult.Failure(new OrderError(
            error.PropertyName,
            error.Code == ReservationErrorCode.AccountMismatch
                ? OrderErrorCode.AccountMismatch
                : OrderErrorCode.ReservationNotActive,
            error.Message));
    }

    private static OrderOperationResult InvalidState(string message) =>
        OrderOperationResult.Failure(new OrderError(
            nameof(Status),
            OrderErrorCode.InvalidState,
            message));

    private static OrderOperationResult InvalidTimestamp(string propertyName) =>
        OrderOperationResult.Failure(new OrderError(
            propertyName,
            OrderErrorCode.InvalidTimestamp,
            "Timestamp must use UTC and cannot be earlier than order creation."));

    private static bool IsUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero;
}
