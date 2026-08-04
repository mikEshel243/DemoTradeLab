using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Core.Analytics;

public sealed class TradeAnalyticsService(ITradeRepository repository)
{
    public async Task<DashboardAnalytics> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var trades = await repository.ListAsync(cancellationToken);
        return TradeAnalyticsCalculator.CalculateDashboard(trades);
    }

    public async Task<IReadOnlyList<InstrumentSummary>> GetInstrumentSummariesAsync(
        CancellationToken cancellationToken)
    {
        var trades = await repository.ListAsync(cancellationToken);
        return TradeAnalyticsCalculator.CalculateInstrumentSummaries(trades);
    }

    public async Task<IReadOnlyList<CurrencyProfitLossTimeline>> GetProfitLossTimelineAsync(
        CancellationToken cancellationToken)
    {
        var trades = await repository.ListAsync(cancellationToken);
        return TradeAnalyticsCalculator.CalculateProfitLossTimeline(trades);
    }
}
