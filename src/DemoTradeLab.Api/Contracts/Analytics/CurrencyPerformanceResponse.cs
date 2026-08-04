namespace DemoTradeLab.Api.Contracts.Analytics;

public sealed record CurrencyPerformanceResponse(
    string Currency,
    decimal TotalRealizedProfitLoss,
    TradeHighlightResponse BestTrade,
    TradeHighlightResponse WorstTrade);
