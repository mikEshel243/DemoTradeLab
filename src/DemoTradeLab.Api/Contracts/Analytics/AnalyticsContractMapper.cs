using DemoTradeLab.Core.Analytics;

namespace DemoTradeLab.Api.Contracts.Analytics;

internal static class AnalyticsContractMapper
{
    public static DashboardResponse ToResponse(this DashboardAnalytics analytics) =>
        new(
            analytics.TotalTrades,
            analytics.ProfitableTrades,
            analytics.LosingTrades,
            analytics.BreakEvenTrades,
            analytics.WinRatePercentage,
            analytics.MostActiveInstrument,
            analytics.AverageTradeDuration is { } duration
                ? ToMinutes(duration)
                : null,
            analytics.CurrencyPerformance
                .Select(performance => performance.ToResponse())
                .ToArray());

    public static InstrumentSummaryResponse ToResponse(this InstrumentSummary summary) =>
        new(
            summary.Instrument,
            summary.Currency,
            summary.TotalTrades,
            summary.ProfitableTrades,
            summary.LosingTrades,
            summary.BreakEvenTrades,
            summary.WinRatePercentage,
            summary.TotalRealizedProfitLoss,
            ToMinutes(summary.AverageTradeDuration));

    public static CurrencyProfitLossTimelineResponse ToResponse(
        this CurrencyProfitLossTimeline timeline) =>
        new(
            timeline.Currency,
            timeline.Points
                .Select(point => new ProfitLossPointResponse(
                    point.TradeId,
                    point.Instrument,
                    point.ClosedAtUtc,
                    point.RealizedProfitLoss,
                    point.CumulativeRealizedProfitLoss))
                .ToArray());

    private static CurrencyPerformanceResponse ToResponse(
        this CurrencyPerformance performance) =>
        new(
            performance.Currency,
            performance.TotalRealizedProfitLoss,
            performance.BestTrade.ToResponse(),
            performance.WorstTrade.ToResponse());

    private static TradeHighlightResponse ToResponse(this TradeHighlight trade) =>
        new(
            trade.Id,
            trade.Instrument,
            trade.ClosedAtUtc,
            trade.RealizedProfitLoss);

    private static decimal ToMinutes(TimeSpan duration) =>
        decimal.Round(
            (decimal)duration.Ticks / TimeSpan.TicksPerMinute,
            decimals: 2,
            MidpointRounding.AwayFromZero);
}
