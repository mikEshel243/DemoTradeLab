using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationService(
    IReservationRepository repository,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<DemoReservation>?> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!await repository.AccountExistsAsync(accountId, cancellationToken))
        {
            return null;
        }

        return await repository.ListAsync(accountId, cancellationToken);
    }

    public Task<DemoReservation?> GetByIdAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        repository.GetByIdAsync(accountId, reservationId, cancellationToken);

    public async Task<ReservationOperationResult> CreateAsync(
        Guid accountId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetAccountForUpdateAsync(
            accountId,
            cancellationToken);

        if (account is null)
        {
            return AccountNotFound(accountId);
        }

        var result = DemoReservation.Create(account, amount, UtcNow());

        if (!result.IsSuccess)
        {
            return result;
        }

        repository.Add(result.Reservation);
        await repository.SaveChangesAsync(cancellationToken);

        return result;
    }

    public Task<ReservationOperationResult> ReleaseAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        CompleteAsync(
            accountId,
            reservationId,
            static (reservation, account, completedAtUtc) =>
                reservation.Release(account, completedAtUtc),
            cancellationToken);

    public Task<ReservationOperationResult> ConsumeAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        CompleteAsync(
            accountId,
            reservationId,
            static (reservation, account, completedAtUtc) =>
                reservation.Consume(account, completedAtUtc),
            cancellationToken);

    private async Task<ReservationOperationResult> CompleteAsync(
        Guid accountId,
        Guid reservationId,
        Func<DemoReservation, DemoAccount, DateTimeOffset,
            ReservationOperationResult> complete,
        CancellationToken cancellationToken)
    {
        var reservation = await repository.GetByIdForUpdateAsync(
            accountId,
            reservationId,
            cancellationToken);

        if (reservation is null)
        {
            return ReservationOperationResult.Failure(new ReservationError(
                nameof(reservationId),
                ReservationErrorCode.ReservationNotFound,
                $"Reservation '{reservationId}' was not found for account '{accountId}'."));
        }

        var account = await repository.GetAccountForUpdateAsync(
            accountId,
            cancellationToken);

        if (account is null)
        {
            return AccountNotFound(accountId);
        }

        var result = complete(reservation, account, UtcNow());

        if (result.IsSuccess)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow();

    private static ReservationOperationResult AccountNotFound(Guid accountId) =>
        ReservationOperationResult.Failure(new ReservationError(
            nameof(accountId),
            ReservationErrorCode.AccountNotFound,
            $"Demo account '{accountId}' was not found."));
}
