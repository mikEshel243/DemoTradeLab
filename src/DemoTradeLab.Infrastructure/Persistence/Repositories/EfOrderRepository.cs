using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Orders;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence.Repositories;

internal sealed class EfOrderRepository(DemoTradeLabDbContext context) : IOrderRepository
{
    public Task<DemoAccount?> GetAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.DemoAccounts.SingleOrDefaultAsync(
            account => account.Id == accountId,
            cancellationToken);

    public Task<DemoReservation?> GetReservationAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        context.DemoReservations.SingleOrDefaultAsync(
            reservation => reservation.Id == reservationId
                && reservation.DemoAccountId == accountId,
            cancellationToken);

    public Task<DemoOrder?> GetByIdAsync(
        Guid accountId,
        Guid orderId,
        bool forUpdate,
        CancellationToken cancellationToken)
    {
        var query = context.DemoOrders.AsQueryable();

        if (!forUpdate)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(
            order => order.Id == orderId && order.DemoAccountId == accountId,
            cancellationToken);
    }

    public Task<DemoOrder?> GetByReservationIdAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        context.DemoOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                order => order.DemoAccountId == accountId
                    && order.ReservationId == reservationId,
                cancellationToken);

    public async Task<IReadOnlyList<DemoOrder>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var orders = await context.DemoOrders
            .AsNoTracking()
            .Where(order => order.DemoAccountId == accountId)
            .ToListAsync(cancellationToken);

        return orders
            .OrderByDescending(order => order.CreatedAtUtc)
            .ThenBy(order => order.Id)
            .ToArray();
    }

    public async Task<IReadOnlyList<DemoOrderEvent>> ListEventsAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var orderEvents = await context.DemoOrderEvents
            .AsNoTracking()
            .Where(orderEvent => orderEvent.OrderId == orderId)
            .ToListAsync(cancellationToken);

        return orderEvents
            .OrderBy(orderEvent => orderEvent.OccurredAtUtc)
            .ThenBy(orderEvent => orderEvent.Id)
            .ToArray();
    }

    public async Task<IReadOnlyList<DemoReservation>> ListActiveReservationsAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await context.DemoReservations
            .AsNoTracking()
            .Where(reservation => reservation.DemoAccountId == accountId
                && reservation.Status == ReservationStatus.Active)
            .ToListAsync(cancellationToken);

    public Task<int> CountFailedOrdersAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.DemoOrders.CountAsync(
            order => order.DemoAccountId == accountId
                && order.Status == OrderStatus.Failed,
            cancellationToken);

    public async Task<IReservationTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfReservationTransaction(transaction);
    }

    public void Add(DemoOrder order)
    {
        context.DemoOrders.Add(order);
    }

    public void Add(DemoOrderEvent orderEvent)
    {
        context.DemoOrderEvents.Add(orderEvent);
    }

    public void Add(ReservationAuditEntry auditEntry)
    {
        context.ReservationAuditEntries.Add(auditEntry);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
