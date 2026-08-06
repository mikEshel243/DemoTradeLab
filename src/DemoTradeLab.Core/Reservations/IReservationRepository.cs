using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Core.Reservations;

public interface IReservationRepository
{
    Task<bool> AccountExistsAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<DemoAccount?> GetAccountForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<DemoReservation?> GetByIdAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<DemoReservation?> GetByIdForUpdateAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DemoReservation>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    void Add(DemoReservation reservation);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
