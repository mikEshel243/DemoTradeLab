namespace DemoTradeLab.Core.Orders;

public sealed record OrderError(
    string PropertyName,
    OrderErrorCode Code,
    string Message);
