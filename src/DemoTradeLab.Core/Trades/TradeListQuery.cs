namespace DemoTradeLab.Core.Trades;

public sealed record TradeListQuery(
    string? Instrument = null,
    string? Currency = null,
    TradeDirection? Direction = null,
    TradeDataSource? Source = null,
    TradeOutcome? Outcome = null,
    DateTimeOffset? ClosedFromUtc = null,
    DateTimeOffset? ClosedToUtc = null,
    TradeSortField SortBy = TradeSortField.ClosedAtUtc,
    TradeSortDirection SortDirection = TradeSortDirection.Descending);
