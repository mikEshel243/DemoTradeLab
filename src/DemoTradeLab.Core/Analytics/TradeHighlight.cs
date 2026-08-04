namespace DemoTradeLab.Core.Analytics;

public sealed record TradeHighlight(
    Guid Id,
    string Instrument,
    DateTimeOffset ClosedAtUtc,
    decimal RealizedProfitLoss);
