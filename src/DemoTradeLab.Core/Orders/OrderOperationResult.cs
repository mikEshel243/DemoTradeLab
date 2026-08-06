using System.Diagnostics.CodeAnalysis;

namespace DemoTradeLab.Core.Orders;

public sealed class OrderOperationResult
{
    private OrderOperationResult(
        DemoOrder? order,
        IReadOnlyList<OrderError> errors,
        bool isNoOp)
    {
        Order = order;
        Errors = errors;
        IsNoOp = isNoOp;
    }

    [MemberNotNullWhen(true, nameof(Order))]
    public bool IsSuccess => Order is not null;

    public DemoOrder? Order { get; }

    public IReadOnlyList<OrderError> Errors { get; }

    public bool IsNoOp { get; }

    internal static OrderOperationResult Success(DemoOrder order, bool isNoOp = false) =>
        new(order, Array.Empty<OrderError>(), isNoOp);

    internal static OrderOperationResult Failure(params OrderError[] errors) =>
        new(null, errors, false);
}
