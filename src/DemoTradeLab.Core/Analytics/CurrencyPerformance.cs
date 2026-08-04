namespace DemoTradeLab.Core.Analytics;

public sealed record CurrencyPerformance(
    string Currency,
    decimal TotalRealizedProfitLoss,
    TradeHighlight BestTrade,
    TradeHighlight WorstTrade);
