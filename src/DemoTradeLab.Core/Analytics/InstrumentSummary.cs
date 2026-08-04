namespace DemoTradeLab.Core.Analytics;

public sealed record InstrumentSummary(
    string Instrument,
    string Currency,
    int TotalTrades,
    int ProfitableTrades,
    int LosingTrades,
    int BreakEvenTrades,
    decimal WinRatePercentage,
    decimal TotalRealizedProfitLoss,
    TimeSpan AverageTradeDuration);
