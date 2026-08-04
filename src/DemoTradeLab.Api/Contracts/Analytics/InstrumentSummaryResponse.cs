namespace DemoTradeLab.Api.Contracts.Analytics;

public sealed record InstrumentSummaryResponse(
    string Instrument,
    string Currency,
    int TotalTrades,
    int ProfitableTrades,
    int LosingTrades,
    int BreakEvenTrades,
    decimal WinRatePercentage,
    decimal TotalRealizedProfitLoss,
    decimal AverageTradeDurationMinutes);
