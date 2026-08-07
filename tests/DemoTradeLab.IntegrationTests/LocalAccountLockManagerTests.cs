using DemoTradeLab.Core.Reservations;
using DemoTradeLab.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests;

public sealed class LocalAccountLockManagerTests
{
    /// <summary>
    /// Acquires the same account key twice and verifies that the second caller waits until the first lease is disposed.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_ForSameAccount_WaitsUntilFirstLeaseIsReleased()
    {
        await using var provider = CreateServiceProvider();
        var lockManager = provider.GetRequiredService<IAccountLockManager>();
        var accountId = Guid.NewGuid();
        var firstLease = await lockManager.AcquireAsync(
            accountId,
            CancellationToken.None);

        var secondLeaseTask = lockManager.AcquireAsync(
            accountId,
            CancellationToken.None);

        Assert.False(secondLeaseTask.IsCompleted);

        await firstLease.DisposeAsync();
        await using var secondLease = await secondLeaseTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Acquires two different account keys and verifies that unrelated accounts do not share one global lock.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_ForDifferentAccounts_UsesIndependentLocks()
    {
        await using var provider = CreateServiceProvider();
        var lockManager = provider.GetRequiredService<IAccountLockManager>();
        await using var firstLease = await lockManager.AcquireAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        var secondLeaseTask = lockManager.AcquireAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        await using var secondLease = await secondLeaseTask.WaitAsync(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Cancels a waiting caller and verifies that cancellation does not lose or permanently block the keyed lock.
    /// </summary>
    [Fact]
    public async Task AcquireAsync_WhenWaitingIsCancelled_DoesNotAbandonAccountLock()
    {
        await using var provider = CreateServiceProvider();
        var lockManager = provider.GetRequiredService<IAccountLockManager>();
        var accountId = Guid.NewGuid();
        var firstLease = await lockManager.AcquireAsync(
            accountId,
            CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var cancelledWait = lockManager.AcquireAsync(accountId, cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledWait);
        await firstLease.DisposeAsync();
        await using var nextLease = await lockManager.AcquireAsync(
            accountId,
            CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Throws while a lease is held and verifies that asynchronous disposal still releases the account lock.
    /// </summary>
    [Fact]
    public async Task AsyncLease_WhenOperationThrows_ReleasesAccountLock()
    {
        await using var provider = CreateServiceProvider();
        var lockManager = provider.GetRequiredService<IAccountLockManager>();
        var accountId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ThrowInsideLeaseAsync(lockManager, accountId));

        await using var nextLease = await lockManager.AcquireAsync(
            accountId,
            CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
    }

    private static async Task ThrowInsideLeaseAsync(
        IAccountLockManager lockManager,
        Guid accountId)
    {
        await using var lease = await lockManager.AcquireAsync(
            accountId,
            CancellationToken.None);
        throw new InvalidOperationException("Controlled test failure.");
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure("Data Source=:memory:");
        return services.BuildServiceProvider();
    }
}
