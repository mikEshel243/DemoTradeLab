using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.UnitTests.Trades;

public sealed class TradeServiceQueryTests
{
    /// <summary>
    /// Filters text without case sensitivity and verifies exact decimal sorting through the trade query use case.
    /// </summary>
    [Fact]
    public async Task ListAsync_FiltersCaseInsensitivelyAndSortsDecimalValuesExactly()
    {
        var lowerProfit = CreateTrade(
            "EUR/USD",
            TradeDirection.Buy,
            TradeDataSource.Manual,
            "USD",
            2.10m,
            openedMinute: 0,
            durationMinutes: 20);
        var higherProfit = CreateTrade(
            "eur/usd",
            TradeDirection.Sell,
            TradeDataSource.Sample,
            "USD",
            10.05m,
            openedMinute: 30,
            durationMinutes: 10);
        var losingTrade = CreateTrade(
            "EUR/USD",
            TradeDirection.Buy,
            TradeDataSource.Manual,
            "USD",
            -1m,
            openedMinute: 60,
            durationMinutes: 5);
        var otherInstrument = CreateTrade(
            "AAPL",
            TradeDirection.Buy,
            TradeDataSource.Manual,
            "USD",
            100m,
            openedMinute: 90,
            durationMinutes: 5);
        var service = new TradeService(new StubTradeRepository(
            [lowerProfit, higherProfit, losingTrade, otherInstrument]));
        var query = new TradeListQuery(
            Instrument: " eur/usd ",
            Currency: "usd",
            Outcome: TradeOutcome.Profitable,
            SortBy: TradeSortField.RealizedProfitLoss,
            SortDirection: TradeSortDirection.Descending);

        var result = await service.ListAsync(query, CancellationToken.None);

        Assert.Equal([higherProfit.Id, lowerProfit.Id], result.Select(trade => trade.Id));
    }

    /// <summary>
    /// Combines direction, source, UTC date, and duration filters and verifies that only matching trades are returned.
    /// </summary>
    [Fact]
    public async Task ListAsync_AppliesDirectionSourceDateAndDurationFilters()
    {
        var matchingTrade = CreateTrade(
            "AAPL",
            TradeDirection.Sell,
            TradeDataSource.Sample,
            "USD",
            5m,
            openedMinute: 30,
            durationMinutes: 15);
        var wrongDirection = CreateTrade(
            "AAPL",
            TradeDirection.Buy,
            TradeDataSource.Sample,
            "USD",
            5m,
            openedMinute: 30,
            durationMinutes: 5);
        var service = new TradeService(new StubTradeRepository(
            [wrongDirection, matchingTrade]));
        var query = new TradeListQuery(
            Direction: TradeDirection.Sell,
            Source: TradeDataSource.Sample,
            ClosedFromUtc: matchingTrade.ClosedAtUtc,
            ClosedToUtc: matchingTrade.ClosedAtUtc,
            SortBy: TradeSortField.Duration,
            SortDirection: TradeSortDirection.Ascending);

        var result = await service.ListAsync(query, CancellationToken.None);

        var trade = Assert.Single(result);
        Assert.Equal(matchingTrade.Id, trade.Id);
    }

    private static Trade CreateTrade(
        string instrument,
        TradeDirection direction,
        TradeDataSource source,
        string currency,
        decimal realizedProfitLoss,
        int openedMinute,
        int durationMinutes)
    {
        var openedAtUtc = new DateTimeOffset(
            2026,
            8,
            4,
            10,
            0,
            0,
            TimeSpan.Zero).AddMinutes(openedMinute);
        var result = Trade.Create(new TradeDraft(
            Instrument: instrument,
            Direction: direction,
            OpenedAtUtc: openedAtUtc,
            ClosedAtUtc: openedAtUtc.AddMinutes(durationMinutes),
            OpeningPrice: 100m,
            ClosingPrice: 101m,
            Quantity: 1m,
            RealizedProfitLoss: realizedProfitLoss,
            Currency: currency,
            Fees: null,
            FinancingCosts: null,
            Source: source,
            ImportedAtUtc: null));

        Assert.True(result.IsSuccess);
        return Assert.IsType<Trade>(result.Trade);
    }

    private sealed class StubTradeRepository(IReadOnlyList<Trade> trades) : ITradeRepository
    {
        public Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult(trades);

        public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Trade?> GetByIdForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void Add(Trade trade) => throw new NotSupportedException();

        public void Remove(Trade trade) => throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
