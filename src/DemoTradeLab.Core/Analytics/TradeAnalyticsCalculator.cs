using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Core.Analytics;

public static class TradeAnalyticsCalculator
{
    public static DashboardAnalytics CalculateDashboard(IReadOnlyList<Trade> trades)
    {
        ArgumentNullException.ThrowIfNull(trades);

        var profitableTrades = trades.Count(IsProfitable);
        var losingTrades = trades.Count(IsLosing);
        var breakEvenTrades = trades.Count(IsBreakEven);
        var currencyPerformance = trades
            .GroupBy(trade => trade.Currency, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(CreateCurrencyPerformance)
            .ToArray();

        return new DashboardAnalytics(
            trades.Count,
            profitableTrades,
            losingTrades,
            breakEvenTrades,
            CalculateWinRate(profitableTrades, trades.Count),
            FindMostActiveInstrument(trades),
            CalculateAverageDuration(trades),
            currencyPerformance);
    }

    public static IReadOnlyList<InstrumentSummary> CalculateInstrumentSummaries(
        IReadOnlyList<Trade> trades)
    {
        ArgumentNullException.ThrowIfNull(trades);

        return trades
            .GroupBy(trade => new
            {
                Instrument = trade.Instrument.ToUpperInvariant(),
                Currency = trade.Currency.ToUpperInvariant()
            })
            .Select(group =>
            {
                var groupedTrades = group.ToArray();
                var profitableTrades = groupedTrades.Count(IsProfitable);
                var losingTrades = groupedTrades.Count(IsLosing);
                var breakEvenTrades = groupedTrades.Count(IsBreakEven);
                var displayInstrument = groupedTrades
                    .Select(trade => trade.Instrument)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .First();

                return new InstrumentSummary(
                    displayInstrument,
                    group.Key.Currency,
                    groupedTrades.Length,
                    profitableTrades,
                    losingTrades,
                    breakEvenTrades,
                    CalculateWinRate(profitableTrades, groupedTrades.Length),
                    groupedTrades.Sum(trade => trade.RealizedProfitLoss),
                    CalculateAverageDuration(groupedTrades)!.Value);
            })
            .OrderByDescending(summary => summary.TotalTrades)
            .ThenBy(summary => summary.Instrument, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Currency, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<CurrencyProfitLossTimeline> CalculateProfitLossTimeline(
        IReadOnlyList<Trade> trades)
    {
        ArgumentNullException.ThrowIfNull(trades);

        return trades
            .GroupBy(trade => trade.Currency, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var cumulativeProfitLoss = 0m;
                var points = group
                    .OrderBy(trade => trade.ClosedAtUtc)
                    .ThenBy(trade => trade.Id)
                    .Select(trade =>
                    {
                        cumulativeProfitLoss += trade.RealizedProfitLoss;

                        return new ProfitLossPoint(
                            trade.Id,
                            trade.Instrument,
                            trade.ClosedAtUtc,
                            trade.RealizedProfitLoss,
                            cumulativeProfitLoss);
                    })
                    .ToArray();

                return new CurrencyProfitLossTimeline(group.Key, points);
            })
            .ToArray();
    }

    private static CurrencyPerformance CreateCurrencyPerformance(
        IGrouping<string, Trade> group)
    {
        var bestTrade = group
            .OrderByDescending(trade => trade.RealizedProfitLoss)
            .ThenBy(trade => trade.ClosedAtUtc)
            .ThenBy(trade => trade.Id)
            .First();
        var worstTrade = group
            .OrderBy(trade => trade.RealizedProfitLoss)
            .ThenBy(trade => trade.ClosedAtUtc)
            .ThenBy(trade => trade.Id)
            .First();

        return new CurrencyPerformance(
            group.Key,
            group.Sum(trade => trade.RealizedProfitLoss),
            ToHighlight(bestTrade),
            ToHighlight(worstTrade));
    }

    private static TradeHighlight ToHighlight(Trade trade) =>
        new(
            trade.Id,
            trade.Instrument,
            trade.ClosedAtUtc,
            trade.RealizedProfitLoss);

    private static string? FindMostActiveInstrument(IReadOnlyList<Trade> trades) =>
        trades
            .GroupBy(trade => trade.Instrument, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                Instrument = group
                    .Select(trade => trade.Instrument)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .First(),
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Instrument, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Instrument, StringComparer.Ordinal)
            .Select(item => item.Instrument)
            .FirstOrDefault();

    private static TimeSpan? CalculateAverageDuration(IReadOnlyCollection<Trade> trades)
    {
        if (trades.Count == 0)
        {
            return null;
        }

        var averageTicks = decimal.Round(
            trades.Average(trade =>
                (decimal)(trade.ClosedAtUtc - trade.OpenedAtUtc).Ticks),
            decimals: 0,
            MidpointRounding.AwayFromZero);

        return TimeSpan.FromTicks((long)averageTicks);
    }

    private static decimal CalculateWinRate(int profitableTrades, int totalTrades) =>
        totalTrades == 0
            ? 0m
            : decimal.Round(
                profitableTrades * 100m / totalTrades,
                decimals: 2,
                MidpointRounding.AwayFromZero);

    private static bool IsProfitable(Trade trade) => trade.RealizedProfitLoss > 0m;

    private static bool IsLosing(Trade trade) => trade.RealizedProfitLoss < 0m;

    private static bool IsBreakEven(Trade trade) => trade.RealizedProfitLoss == 0m;
}
