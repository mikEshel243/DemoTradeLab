namespace DemoTradeLab.Core.Orders;

public sealed record OrderReconciliationReport(
    Guid DemoAccountId,
    decimal TotalBalance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    decimal ActiveReservationTotal,
    bool IsBalanceConsistent,
    int FailedOrdersPendingCompensation);
