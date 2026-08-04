using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Api.Contracts.Trades;

public sealed record TradeResponse(
    Guid Id,
    string Instrument,
    TradeDirection Direction,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset ClosedAtUtc,
    decimal OpeningPrice,
    decimal ClosingPrice,
    decimal Quantity,
    decimal RealizedProfitLoss,
    string Currency,
    decimal? Fees,
    decimal? FinancingCosts,
    TradeDataSource Source,
    DateTimeOffset? ImportedAtUtc);
