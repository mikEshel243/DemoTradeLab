using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationService(
    IReservationRepository repository,
    IAccountLockManager lockManager,
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
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var normalizedKey = idempotencyKey?.Trim() ?? string.Empty;
        var keyError = ValidateIdempotencyKey(normalizedKey);

        if (keyError is not null)
        {
            return ReservationOperationResult.Failure(keyError);
        }

        await using var accountLock = await lockManager.AcquireAsync(
            accountId,
            cancellationToken);
        await using var transaction = await repository.BeginTransactionAsync(
            cancellationToken);

        var replay = await TryReplayAsync(
            accountId,
            amount,
            normalizedKey,
            cancellationToken);

        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return replay;
        }

        var account = await repository.GetAccountForUpdateAsync(
            accountId,
            cancellationToken);

        if (account is null)
        {
            return AccountNotFound(accountId);
        }

        var occurredAtUtc = UtcNow();
        var result = DemoReservation.Create(account, amount, occurredAtUtc);

        if (result.IsSuccess)
        {
            repository.Add(result.Reservation);
            repository.Add(ReservationIdempotencyRecord.Created(
                accountId,
                normalizedKey,
                amount,
                result.Reservation.Id,
                occurredAtUtc));
            repository.Add(ReservationAuditEntry.Create(
                accountId,
                result.Reservation.Id,
                ReservationAuditEventType.Created,
                amount,
                occurredAtUtc));
        }
        else if (result.Errors.Any(
                     error => error.Code == ReservationErrorCode.InsufficientFunds))
        {
            repository.Add(ReservationIdempotencyRecord.InsufficientFunds(
                accountId,
                normalizedKey,
                amount,
                occurredAtUtc));
            repository.Add(ReservationAuditEntry.Create(
                accountId,
                null,
                ReservationAuditEventType.RejectedInsufficientFunds,
                amount,
                occurredAtUtc));
        }
        else
        {
            return result;
        }

        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    public Task<ReservationOperationResult> ReleaseAsync(
        Guid accountId,
        Guid reservationId,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        CompleteAsync(
            accountId,
            reservationId,
            idempotencyKey,
            ReservationCompletionOperation.Release,
            ReservationStatus.Released,
            ReservationAuditEventType.Released,
            static (reservation, account, completedAtUtc) =>
                reservation.Release(account, completedAtUtc),
            cancellationToken);

    public Task<ReservationOperationResult> ConsumeAsync(
        Guid accountId,
        Guid reservationId,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        CompleteAsync(
            accountId,
            reservationId,
            idempotencyKey,
            ReservationCompletionOperation.Consume,
            ReservationStatus.Consumed,
            ReservationAuditEventType.Consumed,
            static (reservation, account, completedAtUtc) =>
                reservation.Consume(account, completedAtUtc),
            cancellationToken);

    private async Task<ReservationOperationResult?> TryReplayAsync(
        Guid accountId,
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var record = await repository.GetIdempotencyRecordAsync(
            accountId,
            idempotencyKey,
            cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (record.RequestedAmount != amount)
        {
            return ReservationOperationResult.Failure(new ReservationError(
                nameof(idempotencyKey),
                ReservationErrorCode.IdempotencyConflict,
                "The idempotency key was already used with a different amount."))
                .AsReplay();
        }

        if (record.Outcome == ReservationIdempotencyOutcome.InsufficientFunds)
        {
            return ReservationOperationResult.Failure(new ReservationError(
                nameof(DemoAccount.AvailableBalance),
                ReservationErrorCode.InsufficientFunds,
                "The original request was rejected because available funds were insufficient."))
                .AsReplay();
        }

        if (record.ReservationId is not { } reservationId)
        {
            throw new InvalidOperationException(
                $"Successful idempotency record '{record.Id}' has no reservation ID.");
        }

        var reservation = await repository.GetByIdAsync(
            accountId,
            reservationId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Reservation '{reservationId}' referenced by idempotency record '{record.Id}' was not found.");

        return ReservationOperationResult.Success(reservation).AsReplay();
    }

    private async Task<ReservationOperationResult> CompleteAsync(
        Guid accountId,
        Guid reservationId,
        string? idempotencyKey,
        ReservationCompletionOperation operation,
        ReservationStatus completedStatus,
        ReservationAuditEventType auditEventType,
        Func<DemoReservation, DemoAccount, DateTimeOffset,
            ReservationOperationResult> complete,
        CancellationToken cancellationToken)
    {
        var normalizedKey = idempotencyKey?.Trim() ?? string.Empty;
        var keyError = ValidateIdempotencyKey(normalizedKey);

        if (keyError is not null)
        {
            return ReservationOperationResult.Failure(keyError);
        }

        await using var accountLock = await lockManager.AcquireAsync(
            accountId,
            cancellationToken);
        await using var transaction = await repository.BeginTransactionAsync(
            cancellationToken);

        var replay = await TryReplayCompletionAsync(
            accountId,
            reservationId,
            normalizedKey,
            operation,
            cancellationToken);

        if (replay is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return replay;
        }

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

        var occurredAtUtc = UtcNow();

        if (reservation.Status == completedStatus)
        {
            repository.Add(ReservationCompletionRecord.Create(
                accountId,
                reservationId,
                normalizedKey,
                operation,
                occurredAtUtc));
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ReservationOperationResult.Success(reservation);
        }

        var account = await repository.GetAccountForUpdateAsync(
            accountId,
            cancellationToken);

        if (account is null)
        {
            return AccountNotFound(accountId);
        }

        var result = complete(reservation, account, occurredAtUtc);

        if (!result.IsSuccess)
        {
            return result;
        }

        repository.Add(ReservationCompletionRecord.Create(
            accountId,
            reservationId,
            normalizedKey,
            operation,
            occurredAtUtc));
        repository.Add(ReservationAuditEntry.Create(
            accountId,
            reservationId,
            auditEventType,
            reservation.Amount,
            occurredAtUtc));
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private async Task<ReservationOperationResult?> TryReplayCompletionAsync(
        Guid accountId,
        Guid reservationId,
        string idempotencyKey,
        ReservationCompletionOperation operation,
        CancellationToken cancellationToken)
    {
        var record = await repository.GetCompletionRecordAsync(
            accountId,
            idempotencyKey,
            cancellationToken);

        if (record is null)
        {
            return null;
        }

        if (record.ReservationId != reservationId || record.Operation != operation)
        {
            return ReservationOperationResult.Failure(new ReservationError(
                nameof(idempotencyKey),
                ReservationErrorCode.IdempotencyConflict,
                "The idempotency key was already used for a different completion operation."))
                .AsReplay();
        }

        var reservation = await repository.GetByIdAsync(
            accountId,
            reservationId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Reservation '{reservationId}' referenced by completion record '{record.Id}' was not found.");

        return ReservationOperationResult.Success(reservation).AsReplay();
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow();

    private static ReservationError? ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return new ReservationError(
                "Idempotency-Key",
                ReservationErrorCode.IdempotencyKeyRequired,
                "Idempotency-Key header is required.");
        }

        if (key.Length > ReservationIdempotencyRecord.MaximumKeyLength)
        {
            return new ReservationError(
                "Idempotency-Key",
                ReservationErrorCode.IdempotencyKeyTooLong,
                $"Idempotency-Key must not exceed {ReservationIdempotencyRecord.MaximumKeyLength} characters.");
        }

        return null;
    }

    private static ReservationOperationResult AccountNotFound(Guid accountId) =>
        ReservationOperationResult.Failure(new ReservationError(
            nameof(accountId),
            ReservationErrorCode.AccountNotFound,
            $"Demo account '{accountId}' was not found."));
}
