namespace DemoTradeLab.Api.Contracts.Analytics;

public sealed record DashboardResponse(
    int TotalTrades,
    int ProfitableTrades,
    int LosingTrades,
    int BreakEvenTrades,
    decimal WinRatePercentage,
    string? MostActiveInstrument,
    decimal? AverageTradeDurationMinutes,
    IReadOnlyList<CurrencyPerformanceResponse> CurrencyPerformance);
