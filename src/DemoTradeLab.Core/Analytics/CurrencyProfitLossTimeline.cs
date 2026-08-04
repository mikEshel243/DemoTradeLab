namespace DemoTradeLab.Core.Analytics;

public sealed record CurrencyProfitLossTimeline(
    string Currency,
    IReadOnlyList<ProfitLossPoint> Points);
