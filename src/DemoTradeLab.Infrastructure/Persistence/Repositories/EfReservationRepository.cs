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

    public void Add(DemoReservation reservation)
    {
        context.DemoReservations.Add(reservation);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
