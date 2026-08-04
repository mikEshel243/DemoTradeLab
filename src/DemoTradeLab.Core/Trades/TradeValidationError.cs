namespace DemoTradeLab.Core.Trades;

public sealed record TradeValidationError(
    string PropertyName,
    TradeValidationCode Code,
    string Message);
