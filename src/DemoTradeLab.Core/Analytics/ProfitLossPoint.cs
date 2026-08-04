namespace DemoTradeLab.Core.Analytics;

public sealed record ProfitLossPoint(
    Guid TradeId,
    string Instrument,
    DateTimeOffset ClosedAtUtc,
    decimal RealizedProfitLoss,
    decimal CumulativeRealizedProfitLoss);
