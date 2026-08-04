namespace DemoTradeLab.Core.Analytics;

public sealed record DashboardAnalytics(
    int TotalTrades,
    int ProfitableTrades,
    int LosingTrades,
    int BreakEvenTrades,
    decimal WinRatePercentage,
    string? MostActiveInstrument,
    TimeSpan? AverageTradeDuration,
    IReadOnlyList<CurrencyPerformance> CurrencyPerformance);
