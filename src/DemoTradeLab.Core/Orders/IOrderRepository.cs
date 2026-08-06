using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.Core.Orders;

public interface IOrderRepository
{
    Task<DemoAccount?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<DemoReservation?> GetReservationAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<DemoOrder?> GetByIdAsync(
        Guid accountId,
        Guid orderId,
        bool forUpdate,
        CancellationToken cancellationToken);

    Task<DemoOrder?> GetByReservationIdAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DemoOrder>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DemoOrderEvent>> ListEventsAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DemoReservation>> ListActiveReservationsAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<int> CountFailedOrdersAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<IReservationTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    void Add(DemoOrder order);

    void Add(DemoOrderEvent orderEvent);

    void Add(ReservationAuditEntry auditEntry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
