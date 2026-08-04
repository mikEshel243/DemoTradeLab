namespace DemoTradeLab.Core.Trades;

public sealed record TradeDraft(
    string? Instrument,
    TradeDirection Direction,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset ClosedAtUtc,
    decimal OpeningPrice,
    decimal ClosingPrice,
    decimal Quantity,
    decimal RealizedProfitLoss,
    string? Currency,
    decimal? Fees,
    decimal? FinancingCosts,
    TradeDataSource Source,
    DateTimeOffset? ImportedAtUtc);
