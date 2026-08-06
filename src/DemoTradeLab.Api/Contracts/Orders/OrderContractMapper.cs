using DemoTradeLab.Core.Orders;

namespace DemoTradeLab.Api.Contracts.Orders;

internal static class OrderContractMapper
{
    public static OrderResponse ToResponse(this DemoOrder order) =>
        new(
            order.Id,
            order.DemoAccountId,
            order.ReservationId,
            order.Amount,
            order.Currency,
            order.Status,
            order.CreatedAtUtc,
            order.UpdatedAtUtc);

    public static OrderEventResponse ToResponse(this DemoOrderEvent orderEvent) =>
        new(
            orderEvent.Id,
            orderEvent.OrderId,
            orderEvent.EventType,
            orderEvent.OccurredAtUtc);

    public static ReconciliationResponse ToResponse(
        this OrderReconciliationReport report) =>
        new(
            report.DemoAccountId,
            report.TotalBalance,
            report.ReservedBalance,
            report.AvailableBalance,
            report.ActiveReservationTotal,
            report.IsBalanceConsistent,
            report.FailedOrdersPendingCompensation);
}
