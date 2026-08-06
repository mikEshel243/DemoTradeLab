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

    Task<ReservationIdempotencyRecord?> GetIdempotencyRecordAsync(
        Guid accountId,
        string key,
        CancellationToken cancellationToken);

    Task<ReservationCompletionRecord?> GetCompletionRecordAsync(
        Guid accountId,
        string key,
        CancellationToken cancellationToken);

    Task<IReservationTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken);

    void Add(DemoReservation reservation);

    void Add(ReservationIdempotencyRecord idempotencyRecord);

    void Add(ReservationCompletionRecord completionRecord);

    void Add(ReservationAuditEntry auditEntry);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
