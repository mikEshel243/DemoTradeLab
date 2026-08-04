using DemoTradeLab.Core.Trades;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.IntegrationTests.Persistence;

public sealed class TradePersistenceTests
{
    [Fact]
    public async Task Migration_CanPersistAndReloadTrade()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DemoTradeLabDbContext>()
            .UseSqlite(connection)
            .Options;

        var creationResult = Trade.Create(CreateValidDraft());
        var trade = Assert.IsType<Trade>(creationResult.Trade);

        await using (var writeContext = new DemoTradeLabDbContext(options))
        {
            await writeContext.Database.MigrateAsync();
            writeContext.Trades.Add(trade);
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = new DemoTradeLabDbContext(options))
        {
            var savedTrade = await readContext.Trades
                .AsNoTracking()
                .SingleAsync();

            Assert.Equal(trade.Id, savedTrade.Id);
            Assert.Equal("EUR/USD", savedTrade.Instrument);
            Assert.Equal(TradeDirection.Buy, savedTrade.Direction);
            Assert.Equal(1.1500m, savedTrade.OpeningPrice);
            Assert.Equal(1.1510m, savedTrade.ClosingPrice);
            Assert.Equal(10m, savedTrade.RealizedProfitLoss);
            Assert.Equal(TradeDataSource.Manual, savedTrade.Source);
        }
    }

    private static TradeDraft CreateValidDraft() => new(
        Instrument: "EUR/USD",
        Direction: TradeDirection.Buy,
        OpenedAtUtc: new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero),
        ClosedAtUtc: new DateTimeOffset(2026, 8, 4, 10, 30, 0, TimeSpan.Zero),
        OpeningPrice: 1.1500m,
        ClosingPrice: 1.1510m,
        Quantity: 1_000m,
        RealizedProfitLoss: 10m,
        Currency: "USD",
        Fees: 0.50m,
        FinancingCosts: null,
        Source: TradeDataSource.Manual,
        ImportedAtUtc: null);
}
