namespace DemoTradeLab.Api.Contracts.Analytics;

public sealed record ProfitLossPointResponse(
    Guid TradeId,
    string Instrument,
    DateTimeOffset ClosedAtUtc,
    decimal RealizedProfitLoss,
    decimal CumulativeRealizedProfitLoss);
