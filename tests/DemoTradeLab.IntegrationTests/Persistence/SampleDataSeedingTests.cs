using DemoTradeLab.Core.Trades;
using DemoTradeLab.Infrastructure;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests.Persistence;

public sealed class SampleDataSeedingTests
{
    [Fact]
    public async Task MigrateAsync_OnEmptyDatabase_SeedsSampleTradesOnlyOnce()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"demotrade-lab-seeding-{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            services.AddInfrastructure($"Data Source={databasePath}");

            await using var serviceProvider = services.BuildServiceProvider();
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();

            await context.Database.MigrateAsync();
            await context.Database.MigrateAsync();

            var trades = await context.Trades
                .AsNoTracking()
                .ToListAsync();

            Assert.Equal(8, trades.Count);
            Assert.All(trades, trade => Assert.Equal(TradeDataSource.Sample, trade.Source));
            Assert.Equal(5, trades.Count(trade => trade.RealizedProfitLoss > 0m));
            Assert.Equal(3, trades.Count(trade => trade.RealizedProfitLoss < 0m));
            Assert.Equal(124m, trades.Sum(trade => trade.RealizedProfitLoss));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }
}
