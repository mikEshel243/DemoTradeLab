using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Infrastructure;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests.Persistence;

public sealed class DemoProfileSeedingTests
{
    /// <summary>
    /// Repeats profile initialization and verifies that missing records are added without resetting persisted account balances.
    /// </summary>
    [Fact]
    public async Task MigrateAsync_SeedsProfilesOnceAndPreservesPersistedBalances()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"demotrade-lab-profile-seeding-{Guid.NewGuid():N}.db");
        var seeds = CreateSeeds();

        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure($"Data Source={databasePath}", seeds);

            await using var serviceProvider = services.BuildServiceProvider();
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();

            await context.Database.MigrateAsync();

            var accountId = await context.DemoAccounts
                .Select(account => account.Id)
                .SingleAsync();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE DemoAccounts SET TotalBalance = {125m}, ReservedBalance = {25m} WHERE Id = {accountId}");

            context.ChangeTracker.Clear();
            await context.Database.MigrateAsync();

            var profiles = await context.DemoProfiles
                .AsNoTracking()
                .Include(profile => profile.Accounts)
                .ToListAsync();
            var profile = Assert.Single(profiles);
            var account = Assert.Single(profile.Accounts);

            Assert.Equal("primary-demo", profile.Key);
            Assert.Equal(125m, account.TotalBalance);
            Assert.Equal(25m, account.ReservedBalance);
            Assert.Equal(100m, account.AvailableBalance);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    private static IReadOnlyList<DemoProfileSeed> CreateSeeds() =>
    [
        new DemoProfileSeed(
            new DemoProfileDraft("primary-demo", "Primary Demo Profile"),
            [
                new DemoAccountDraft(
                    "main-account",
                    "Main Demo Account",
                    100m,
                    "USD")
            ])
    ];
}
