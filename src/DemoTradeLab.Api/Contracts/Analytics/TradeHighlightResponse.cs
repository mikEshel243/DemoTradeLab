namespace DemoTradeLab.Api.Contracts.Analytics;

public sealed record TradeHighlightResponse(
    Guid Id,
    string Instrument,
    DateTimeOffset ClosedAtUtc,
    decimal RealizedProfitLoss);
