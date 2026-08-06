using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.UnitTests.Reservations;

public sealed class ReservationServiceTests
{
    private static readonly DateTimeOffset Now = new(
        2026,
        8,
        6,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithExistingAccount_PersistsAtomicOperationRecords()
    {
        var account = CreateAccount();
        var repository = new InMemoryReservationRepository(account);
        var lockManager = new ImmediateLockManager();
        var service = CreateService(repository, lockManager);

        var result = await service.CreateAsync(
            account.Id,
            80m,
            "request-1",
            CancellationToken.None);

        var reservation = Assert.IsType<DemoReservation>(result.Reservation);
        Assert.Equal(Now, reservation.CreatedAtUtc);
        Assert.Equal(80m, account.ReservedBalance);
        Assert.Same(reservation, Assert.Single(repository.Reservations));
        Assert.Equal(
            ReservationIdempotencyOutcome.Created,
            Assert.Single(repository.IdempotencyRecords).Outcome);
        Assert.Equal(
            ReservationAuditEventType.Created,
            Assert.Single(repository.AuditEntries).EventType);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(account.Id, Assert.Single(lockManager.AcquiredAccountIds));
    }

    [Fact]
    public async Task CreateAsync_WithSameKey_ReplaysOriginalSuccessWithoutSavingAgain()
    {
        var account = CreateAccount();
        var repository = new InMemoryReservationRepository(account);
        var service = CreateService(repository, new ImmediateLockManager());
        var first = await service.CreateAsync(
            account.Id,
            80m,
            "request-1",
            CancellationToken.None);

        var replay = await service.CreateAsync(
            account.Id,
            80m,
            "request-1",
            CancellationToken.None);

        Assert.True(replay.IsReplay);
        Assert.Equal(first.Reservation?.Id, replay.Reservation?.Id);
        Assert.Single(repository.Reservations);
        Assert.Single(repository.IdempotencyRecords);
        Assert.Single(repository.AuditEntries);
        Assert.Equal(80m, account.ReservedBalance);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(2, repository.CommitCalls);
    }

    [Fact]
    public async Task CreateAsync_WithReusedKeyAndDifferentAmount_ReturnsConflict()
    {
        var account = CreateAccount();
        var repository = new InMemoryReservationRepository(account);
        var service = CreateService(repository, new ImmediateLockManager());
        await service.CreateAsync(
            account.Id,
            80m,
            "request-1",
            CancellationToken.None);

        var conflict = await service.CreateAsync(
            account.Id,
            70m,
            "request-1",
            CancellationToken.None);

        Assert.True(conflict.IsReplay);
        Assert.Contains(
            conflict.Errors,
            error => error.Code == ReservationErrorCode.IdempotencyConflict);
        Assert.Single(repository.Reservations);
        Assert.Equal(80m, account.ReservedBalance);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_WithInsufficientFunds_PersistsAndReplaysRejection()
    {
        var account = CreateAccount();
        var repository = new InMemoryReservationRepository(account);
        var service = CreateService(repository, new ImmediateLockManager());

        var first = await service.CreateAsync(
            account.Id,
            120m,
            "request-1",
            CancellationToken.None);
        var replay = await service.CreateAsync(
            account.Id,
            120m,
            "request-1",
            CancellationToken.None);

        Assert.Contains(
            first.Errors,
            error => error.Code == ReservationErrorCode.InsufficientFunds);
        Assert.True(replay.IsReplay);
        Assert.Empty(repository.Reservations);
        Assert.Equal(
            ReservationIdempotencyOutcome.InsufficientFunds,
            Assert.Single(repository.IdempotencyRecords).Outcome);
        Assert.Equal(
            ReservationAuditEventType.RejectedInsufficientFunds,
            Assert.Single(repository.AuditEntries).EventType);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(2, repository.CommitCalls);
    }

    [Fact]
    public async Task CreateAsync_WithoutIdempotencyKey_RejectsBeforeLockOrTransaction()
    {
        var account = CreateAccount();
        var repository = new InMemoryReservationRepository(account);
        var lockManager = new ImmediateLockManager();
        var service = CreateService(repository, lockManager);

        var result = await service.CreateAsync(
            account.Id,
            80m,
            null,
            CancellationToken.None);

        Assert.Contains(
            result.Errors,
            error => error.Code == ReservationErrorCode.IdempotencyKeyRequired);
        Assert.Empty(lockManager.AcquiredAccountIds);
        Assert.Equal(0, repository.BeginTransactionCalls);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_WithMissingAccount_ReturnsExpectedFailureWithoutSaving()
    {
        var repository = new InMemoryReservationRepository(null);
        var service = CreateService(repository, new ImmediateLockManager());

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            80m,
            "request-1",
            CancellationToken.None);

        Assert.Contains(
            result.Errors,
            error => error.Code == ReservationErrorCode.AccountNotFound);
        Assert.Empty(repository.Reservations);
        Assert.Equal(0, repository.SaveChangesCalls);
        Assert.Equal(0, repository.CommitCalls);
    }

    private static ReservationService CreateService(
        IReservationRepository repository,
        IAccountLockManager lockManager) =>
        new(repository, lockManager, new FixedTimeProvider(Now));

    private static DemoAccount CreateAccount()
    {
        var profile = Assert.IsType<DemoProfile>(DemoProfile.Create(
            new DemoProfileDraft("demo", "Demo")).Profile);
        return Assert.IsType<DemoAccount>(profile.AddAccount(new DemoAccountDraft(
            "main-account",
            "Main Account",
            100m,
            "USD")).Account);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class ImmediateLockManager : IAccountLockManager
    {
        public List<Guid> AcquiredAccountIds { get; } = [];

        public Task<IAccountLockLease> AcquireAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            AcquiredAccountIds.Add(accountId);
            return Task.FromResult<IAccountLockLease>(new ImmediateLease());
        }

        private sealed class ImmediateLease : IAccountLockLease
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class InMemoryReservationRepository(DemoAccount? account)
        : IReservationRepository
    {
        public List<DemoReservation> Reservations { get; } = [];

        public List<ReservationIdempotencyRecord> IdempotencyRecords { get; } = [];

        public List<ReservationAuditEntry> AuditEntries { get; } = [];

        public List<ReservationCompletionRecord> CompletionRecords { get; } = [];

        public int BeginTransactionCalls { get; private set; }

        public int CommitCalls { get; private set; }

        public int SaveChangesCalls { get; private set; }

        public Task<bool> AccountExistsAsync(
            Guid accountId,
            CancellationToken cancellationToken) =>
            Task.FromResult(account?.Id == accountId);

        public Task<DemoAccount?> GetAccountForUpdateAsync(
            Guid accountId,
            CancellationToken cancellationToken) =>
            Task.FromResult(account?.Id == accountId ? account : null);

        public Task<DemoReservation?> GetByIdAsync(
            Guid accountId,
            Guid reservationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Reservations.SingleOrDefault(
                reservation => reservation.DemoAccountId == accountId
                    && reservation.Id == reservationId));

        public Task<DemoReservation?> GetByIdForUpdateAsync(
            Guid accountId,
            Guid reservationId,
            CancellationToken cancellationToken) =>
            GetByIdAsync(accountId, reservationId, cancellationToken);

        public Task<IReadOnlyList<DemoReservation>> ListAsync(
            Guid accountId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DemoReservation>>(
                Reservations
                    .Where(reservation => reservation.DemoAccountId == accountId)
                    .ToArray());

        public Task<ReservationIdempotencyRecord?> GetIdempotencyRecordAsync(
            Guid accountId,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult(IdempotencyRecords.SingleOrDefault(
                record => record.DemoAccountId == accountId && record.Key == key));

        public Task<ReservationCompletionRecord?> GetCompletionRecordAsync(
            Guid accountId,
            string key,
            CancellationToken cancellationToken) =>
            Task.FromResult(CompletionRecords.SingleOrDefault(
                record => record.DemoAccountId == accountId && record.Key == key));

        public Task<IReservationTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken)
        {
            BeginTransactionCalls++;
            return Task.FromResult<IReservationTransaction>(
                new InMemoryTransaction(() => CommitCalls++));
        }

        public void Add(DemoReservation reservation)
        {
            Reservations.Add(reservation);
        }

        public void Add(ReservationIdempotencyRecord idempotencyRecord)
        {
            IdempotencyRecords.Add(idempotencyRecord);
        }

        public void Add(ReservationCompletionRecord completionRecord)
        {
            CompletionRecords.Add(completionRecord);
        }

        public void Add(ReservationAuditEntry auditEntry)
        {
            AuditEntries.Add(auditEntry);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }

        private sealed class InMemoryTransaction(Action onCommit) : IReservationTransaction
        {
            public Task CommitAsync(CancellationToken cancellationToken)
            {
                onCommit();
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
