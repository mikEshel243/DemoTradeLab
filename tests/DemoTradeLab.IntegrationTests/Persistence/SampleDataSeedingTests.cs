using DemoTradeLab.Core.Analytics;
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

            var dashboard = TradeAnalyticsCalculator.CalculateDashboard(trades);
            var usdPerformance = Assert.Single(dashboard.CurrencyPerformance);

            Assert.Equal(8, dashboard.TotalTrades);
            Assert.Equal(5, dashboard.ProfitableTrades);
            Assert.Equal(3, dashboard.LosingTrades);
            Assert.Equal(0, dashboard.BreakEvenTrades);
            Assert.Equal(62.5m, dashboard.WinRatePercentage);
            Assert.Equal("EUR/USD", dashboard.MostActiveInstrument);
            Assert.Equal(18d, dashboard.AverageTradeDuration?.TotalMinutes);
            Assert.Equal("USD", usdPerformance.Currency);
            Assert.Equal(124m, usdPerformance.TotalRealizedProfitLoss);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }
}
