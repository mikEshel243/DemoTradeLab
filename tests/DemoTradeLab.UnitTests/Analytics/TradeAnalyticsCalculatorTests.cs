using DemoTradeLab.Core.Analytics;
using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.UnitTests.Analytics;

public sealed class TradeAnalyticsCalculatorTests
{
    /// <summary>
    /// Calculates a dashboard with no trades and verifies meaningful zero counts and empty grouped statistics.
    /// </summary>
    [Fact]
    public void CalculateDashboard_WithNoTrades_ReturnsEmptyStatistics()
    {
        var dashboard = TradeAnalyticsCalculator.CalculateDashboard([]);

        Assert.Equal(0, dashboard.TotalTrades);
        Assert.Equal(0m, dashboard.WinRatePercentage);
        Assert.Null(dashboard.MostActiveInstrument);
        Assert.Null(dashboard.AverageTradeDuration);
        Assert.Empty(dashboard.CurrencyPerformance);
        Assert.Empty(TradeAnalyticsCalculator.CalculateInstrumentSummaries([]));
        Assert.Empty(TradeAnalyticsCalculator.CalculateProfitLossTimeline([]));
    }

    /// <summary>
    /// Classifies profitable, losing, and break-even trades while verifying that monetary totals stay separated by currency.
    /// </summary>
    [Fact]
    public void CalculateDashboard_ClassifiesOutcomesAndSeparatesCurrencies()
    {
        var trades = new[]
        {
            CreateTrade("EUR/USD", "USD", 10m, openedMinute: 0, durationMinutes: 10),
            CreateTrade("eur/usd", "USD", -4m, openedMinute: 20, durationMinutes: 20),
            CreateTrade("AAPL", "USD", 0m, openedMinute: 50, durationMinutes: 30),
            CreateTrade("GBP/USD", "EUR", 7m, openedMinute: 90, durationMinutes: 40)
        };

        var dashboard = TradeAnalyticsCalculator.CalculateDashboard(trades);

        Assert.Equal(4, dashboard.TotalTrades);
        Assert.Equal(2, dashboard.ProfitableTrades);
        Assert.Equal(1, dashboard.LosingTrades);
        Assert.Equal(1, dashboard.BreakEvenTrades);
        Assert.Equal(50m, dashboard.WinRatePercentage);
        Assert.Equal("EUR/USD", dashboard.MostActiveInstrument);
        Assert.Equal(TimeSpan.FromMinutes(25), dashboard.AverageTradeDuration);

        var euro = Assert.Single(
            dashboard.CurrencyPerformance,
            performance => performance.Currency == "EUR");
        Assert.Equal(7m, euro.TotalRealizedProfitLoss);

        var usd = Assert.Single(
            dashboard.CurrencyPerformance,
            performance => performance.Currency == "USD");
        Assert.Equal(6m, usd.TotalRealizedProfitLoss);
        Assert.Equal(10m, usd.BestTrade.RealizedProfitLoss);
        Assert.Equal(-4m, usd.WorstTrade.RealizedProfitLoss);
    }

    /// <summary>
    /// Gives two instruments equal activity and verifies that the deterministic alphabetical tie-break is applied.
    /// </summary>
    [Fact]
    public void CalculateDashboard_WhenInstrumentCountsTie_UsesAlphabeticalTieBreak()
    {
        var trades = new[]
        {
            CreateTrade("XAU/USD", "USD", 1m, openedMinute: 0, durationMinutes: 5),
            CreateTrade("AAPL", "USD", 1m, openedMinute: 10, durationMinutes: 5)
        };

        var dashboard = TradeAnalyticsCalculator.CalculateDashboard(trades);

        Assert.Equal("AAPL", dashboard.MostActiveInstrument);
    }

    /// <summary>
    /// Groups instrument names without case sensitivity while verifying that different currencies remain separate groups.
    /// </summary>
    [Fact]
    public void CalculateInstrumentSummaries_GroupsCaseInsensitivelyButKeepsCurrenciesSeparate()
    {
        var trades = new[]
        {
            CreateTrade("EUR/USD", "USD", 10m, openedMinute: 0, durationMinutes: 10),
            CreateTrade("eur/usd", "USD", -5m, openedMinute: 20, durationMinutes: 20),
            CreateTrade("EUR/USD", "EUR", 3m, openedMinute: 50, durationMinutes: 30)
        };

        var summaries = TradeAnalyticsCalculator.CalculateInstrumentSummaries(trades);

        Assert.Equal(2, summaries.Count);

        var usd = Assert.Single(summaries, summary => summary.Currency == "USD");
        Assert.Equal(2, usd.TotalTrades);
        Assert.Equal(50m, usd.WinRatePercentage);
        Assert.Equal(5m, usd.TotalRealizedProfitLoss);
        Assert.Equal(TimeSpan.FromMinutes(15), usd.AverageTradeDuration);

        var euro = Assert.Single(summaries, summary => summary.Currency == "EUR");
        Assert.Equal(1, euro.TotalTrades);
        Assert.Equal(3m, euro.TotalRealizedProfitLoss);
    }

    /// <summary>
    /// Builds a timeline from out-of-order trades and verifies chronological ordering and cumulative profit/loss values.
    /// </summary>
    [Fact]
    public void CalculateProfitLossTimeline_OrdersByClosingTimeAndBuildsCumulativeValue()
    {
        var laterTrade = CreateTrade(
            "AAPL",
            "USD",
            -4m,
            openedMinute: 40,
            durationMinutes: 10);
        var earlierTrade = CreateTrade(
            "EUR/USD",
            "USD",
            10m,
            openedMinute: 0,
            durationMinutes: 10);

        var timelines = TradeAnalyticsCalculator.CalculateProfitLossTimeline(
            [laterTrade, earlierTrade]);

        var timeline = Assert.Single(timelines);
        Assert.Collection(
            timeline.Points,
            point =>
            {
                Assert.Equal(earlierTrade.Id, point.TradeId);
                Assert.Equal(10m, point.CumulativeRealizedProfitLoss);
            },
            point =>
            {
                Assert.Equal(laterTrade.Id, point.TradeId);
                Assert.Equal(6m, point.CumulativeRealizedProfitLoss);
            });
    }

    private static Trade CreateTrade(
        string instrument,
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
            Direction: TradeDirection.Buy,
            OpenedAtUtc: openedAtUtc,
            ClosedAtUtc: openedAtUtc.AddMinutes(durationMinutes),
            OpeningPrice: 100m,
            ClosingPrice: 101m,
            Quantity: 1m,
            RealizedProfitLoss: realizedProfitLoss,
            Currency: currency,
            Fees: null,
            FinancingCosts: null,
            Source: TradeDataSource.Manual,
            ImportedAtUtc: null));

        Assert.True(result.IsSuccess);
        return Assert.IsType<Trade>(result.Trade);
    }
}
