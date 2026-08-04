using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Api.Contracts.Trades;

internal static class TradeContractMapper
{
    public static TradeDraft ToDraft(
        this SaveTradeRequest request,
        TradeDataSource source,
        DateTimeOffset? importedAtUtc = null) =>
        new(
            Instrument: request.Instrument,
            Direction: request.Direction.GetValueOrDefault(),
            OpenedAtUtc: request.OpenedAtUtc.GetValueOrDefault(),
            ClosedAtUtc: request.ClosedAtUtc.GetValueOrDefault(),
            OpeningPrice: request.OpeningPrice.GetValueOrDefault(),
            ClosingPrice: request.ClosingPrice.GetValueOrDefault(),
            Quantity: request.Quantity.GetValueOrDefault(),
            RealizedProfitLoss: request.RealizedProfitLoss.GetValueOrDefault(),
            Currency: request.Currency,
            Fees: request.Fees,
            FinancingCosts: request.FinancingCosts,
            Source: source,
            ImportedAtUtc: importedAtUtc);

    public static TradeResponse ToResponse(this Trade trade) =>
        new(
            trade.Id,
            trade.Instrument,
            trade.Direction,
            trade.OpenedAtUtc,
            trade.ClosedAtUtc,
            trade.OpeningPrice,
            trade.ClosingPrice,
            trade.Quantity,
            trade.RealizedProfitLoss,
            trade.Currency,
            trade.Fees,
            trade.FinancingCosts,
            trade.Source,
            trade.ImportedAtUtc);
}
