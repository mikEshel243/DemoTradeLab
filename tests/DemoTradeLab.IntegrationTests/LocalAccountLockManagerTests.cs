using DemoTradeLab.Core.Reservations;
using DemoTradeLab.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests;

public sealed class LocalAccountLockManagerTests
{
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

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure("Data Source=:memory:");
        return services.BuildServiceProvider();
    }
}
