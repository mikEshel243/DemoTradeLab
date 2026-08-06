using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence.Repositories;

internal sealed class EfReservationRepository(DemoTradeLabDbContext context)
    : IReservationRepository
{
    public Task<bool> AccountExistsAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.DemoAccounts.AnyAsync(
            account => account.Id == accountId,
            cancellationToken);

    public Task<DemoAccount?> GetAccountForUpdateAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.DemoAccounts.SingleOrDefaultAsync(
            account => account.Id == accountId,
            cancellationToken);

    public Task<DemoReservation?> GetByIdAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        context.DemoReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                reservation => reservation.Id == reservationId
                    && reservation.DemoAccountId == accountId,
                cancellationToken);

    public Task<DemoReservation?> GetByIdForUpdateAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        context.DemoReservations.SingleOrDefaultAsync(
            reservation => reservation.Id == reservationId
                && reservation.DemoAccountId == accountId,
            cancellationToken);

    public async Task<IReadOnlyList<DemoReservation>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var reservations = await context.DemoReservations
            .AsNoTracking()
            .Where(reservation => reservation.DemoAccountId == accountId)
            .ToListAsync(cancellationToken);

        return reservations
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .ThenBy(reservation => reservation.Id)
            .ToArray();
    }

    public Task<ReservationIdempotencyRecord?> GetIdempotencyRecordAsync(
        Guid accountId,
        string key,
        CancellationToken cancellationToken) =>
        context.ReservationIdempotencyRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(
                record => record.DemoAccountId == accountId && record.Key == key,
                cancellationToken);

    public async Task<IReservationTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfReservationTransaction(transaction);
    }

    public void Add(DemoReservation reservation)
    {
        context.DemoReservations.Add(reservation);
    }

    public void Add(ReservationIdempotencyRecord idempotencyRecord)
    {
        context.ReservationIdempotencyRecords.Add(idempotencyRecord);
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
