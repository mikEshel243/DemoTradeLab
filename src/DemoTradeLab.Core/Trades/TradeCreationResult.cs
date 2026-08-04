namespace DemoTradeLab.Core.Trades;

public sealed class TradeCreationResult
{
    private TradeCreationResult(Trade? trade, IReadOnlyList<TradeValidationError> errors)
    {
        Trade = trade;
        Errors = errors;
    }

    public bool IsSuccess => Trade is not null;

    public Trade? Trade { get; }

    public IReadOnlyList<TradeValidationError> Errors { get; }

    internal static TradeCreationResult Success(Trade trade) =>
        new(trade, Array.Empty<TradeValidationError>());

    internal static TradeCreationResult Failure(IEnumerable<TradeValidationError> errors) =>
        new(null, errors.ToArray());
}
