using System.Diagnostics.CodeAnalysis;

namespace DemoTradeLab.Core.Trades;

public sealed class TradeUpdateResult
{
    private TradeUpdateResult(Trade? trade, IReadOnlyList<TradeValidationError> errors)
    {
        Trade = trade;
        Errors = errors;
    }

    [MemberNotNullWhen(true, nameof(Trade))]
    public bool IsSuccess => Trade is not null;

    public Trade? Trade { get; }

    public IReadOnlyList<TradeValidationError> Errors { get; }

    internal static TradeUpdateResult Success(Trade trade) =>
        new(trade, Array.Empty<TradeValidationError>());

    internal static TradeUpdateResult Failure(IEnumerable<TradeValidationError> errors) =>
        new(null, errors.ToArray());
}
