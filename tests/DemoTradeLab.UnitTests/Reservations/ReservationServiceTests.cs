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
    public async Task CreateAsync_WithExistingAccount_OrchestratesDomainAndPersistence()
    {
        var account = CreateAccount();
        var repository = new InMemoryReservationRepository(account);
        var service = new ReservationService(repository, new FixedTimeProvider(Now));

        var result = await service.CreateAsync(account.Id, 80m, CancellationToken.None);

        var reservation = Assert.IsType<DemoReservation>(result.Reservation);
        Assert.Equal(Now, reservation.CreatedAtUtc);
        Assert.Equal(80m, account.ReservedBalance);
        Assert.Same(reservation, Assert.Single(repository.Reservations));
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task CreateAsync_WithMissingAccount_ReturnsExpectedFailureWithoutSaving()
    {
        var repository = new InMemoryReservationRepository(null);
        var service = new ReservationService(repository, new FixedTimeProvider(Now));

        var result = await service.CreateAsync(Guid.NewGuid(), 80m, CancellationToken.None);

        Assert.Contains(
            result.Errors,
            error => error.Code == ReservationErrorCode.AccountNotFound);
        Assert.Empty(repository.Reservations);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

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

    private sealed class InMemoryReservationRepository(DemoAccount? account)
        : IReservationRepository
    {
        public List<DemoReservation> Reservations { get; } = [];

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

        public void Add(DemoReservation reservation)
        {
            Reservations.Add(reservation);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }
}
