using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.Core.Orders;

public sealed class OrderService(
    IOrderRepository repository,
    IAccountLockManager lockManager,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<DemoOrder>?> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await repository.GetAccountAsync(accountId, cancellationToken) is null)
        {
            return null;
        }

        return await repository.ListAsync(accountId, cancellationToken);
    }

    public Task<DemoOrder?> GetByIdAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        repository.GetByIdAsync(accountId, orderId, forUpdate: false, cancellationToken);

    public async Task<IReadOnlyList<DemoOrderEvent>?> ListEventsAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (await GetByIdAsync(accountId, orderId, cancellationToken) is null)
        {
            return null;
        }

        return await repository.ListEventsAsync(orderId, cancellationToken);
    }

    public async Task<OrderOperationResult> CreateAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        await using var accountLock = await lockManager.AcquireAsync(
            accountId,
            cancellationToken);
        await using var transaction = await repository.BeginTransactionAsync(
            cancellationToken);

        var existingOrder = await repository.GetByReservationIdAsync(
            accountId,
            reservationId,
            cancellationToken);

        if (existingOrder is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return OrderOperationResult.Success(existingOrder, isNoOp: true);
        }

        var reservation = await repository.GetReservationAsync(
            accountId,
            reservationId,
            cancellationToken);

        if (reservation is null)
        {
            return ReservationNotFound(reservationId);
        }

        var occurredAtUtc = UtcNow();
        var result = DemoOrder.Create(reservation, occurredAtUtc);

        if (!result.IsSuccess)
        {
            return result;
        }

        repository.Add(result.Order);
        repository.Add(DemoOrderEvent.Create(
            result.Order.Id,
            OrderEventType.Created,
            occurredAtUtc));
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    public Task<OrderOperationResult> CompleteAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        TransitionReservationAsync(
            accountId,
            orderId,
            OrderEventType.Completed,
            ReservationAuditEventType.Consumed,
            static (order, reservation, account, occurredAtUtc) =>
                order.Complete(reservation, account, occurredAtUtc),
            cancellationToken);

    public async Task<OrderOperationResult> MarkFailedAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        await using var accountLock = await lockManager.AcquireAsync(
            accountId,
            cancellationToken);
        await using var transaction = await repository.BeginTransactionAsync(
            cancellationToken);
        var order = await repository.GetByIdAsync(
            accountId,
            orderId,
            forUpdate: true,
            cancellationToken);

        if (order is null)
        {
            return OrderNotFound(orderId);
        }

        var occurredAtUtc = UtcNow();
        var result = order.MarkFailed(occurredAtUtc);

        if (!result.IsSuccess || result.IsNoOp)
        {
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        repository.Add(DemoOrderEvent.Create(
            orderId,
            OrderEventType.Failed,
            occurredAtUtc));
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    public Task<OrderOperationResult> CompensateAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        TransitionReservationAsync(
            accountId,
            orderId,
            OrderEventType.Compensated,
            ReservationAuditEventType.Released,
            static (order, reservation, account, occurredAtUtc) =>
                order.Compensate(reservation, account, occurredAtUtc),
            cancellationToken);

    public async Task<OrderReconciliationReport?> ReconcileAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await using var accountLock = await lockManager.AcquireAsync(
            accountId,
            cancellationToken);
        await using var transaction = await repository.BeginTransactionAsync(
            cancellationToken);
        var account = await repository.GetAccountAsync(accountId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        var activeReservations = await repository.ListActiveReservationsAsync(
            accountId,
            cancellationToken);
        var failedOrderCount = await repository.CountFailedOrdersAsync(
            accountId,
            cancellationToken);
        var activeReservationTotal = activeReservations.Sum(
            reservation => reservation.Amount);
        await transaction.CommitAsync(cancellationToken);

        return new OrderReconciliationReport(
            account.Id,
            account.TotalBalance,
            account.ReservedBalance,
            account.AvailableBalance,
            activeReservationTotal,
            account.ReservedBalance == activeReservationTotal,
            failedOrderCount);
    }

    private async Task<OrderOperationResult> TransitionReservationAsync(
        Guid accountId,
        Guid orderId,
        OrderEventType orderEventType,
        ReservationAuditEventType reservationAuditEventType,
        Func<DemoOrder, DemoReservation, DemoAccount, DateTimeOffset,
            OrderOperationResult> transition,
        CancellationToken cancellationToken)
    {
        await using var accountLock = await lockManager.AcquireAsync(
            accountId,
            cancellationToken);
        await using var transaction = await repository.BeginTransactionAsync(
            cancellationToken);
        var order = await repository.GetByIdAsync(
            accountId,
            orderId,
            forUpdate: true,
            cancellationToken);

        if (order is null)
        {
            return OrderNotFound(orderId);
        }

        var reservation = await repository.GetReservationAsync(
            accountId,
            order.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            return ReservationNotFound(order.ReservationId);
        }

        var account = await repository.GetAccountAsync(accountId, cancellationToken);

        if (account is null)
        {
            return AccountNotFound(accountId);
        }

        var occurredAtUtc = UtcNow();
        var result = transition(order, reservation, account, occurredAtUtc);

        if (!result.IsSuccess || result.IsNoOp)
        {
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        repository.Add(DemoOrderEvent.Create(orderId, orderEventType, occurredAtUtc));
        repository.Add(ReservationAuditEntry.Create(
            accountId,
            reservation.Id,
            reservationAuditEventType,
            reservation.Amount,
            occurredAtUtc));
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow();

    private static OrderOperationResult AccountNotFound(Guid accountId) =>
        OrderOperationResult.Failure(new OrderError(
            nameof(accountId),
            OrderErrorCode.AccountNotFound,
            $"Demo account '{accountId}' was not found."));

    private static OrderOperationResult ReservationNotFound(Guid reservationId) =>
        OrderOperationResult.Failure(new OrderError(
            nameof(reservationId),
            OrderErrorCode.ReservationNotFound,
            $"Reservation '{reservationId}' was not found."));

    private static OrderOperationResult OrderNotFound(Guid orderId) =>
        OrderOperationResult.Failure(new OrderError(
            nameof(orderId),
            OrderErrorCode.OrderNotFound,
            $"Order '{orderId}' was not found."));
}
