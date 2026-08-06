namespace DemoTradeLab.Api.Contracts.Orders;

public sealed record ReconciliationResponse(
    Guid DemoAccountId,
    decimal TotalBalance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    decimal ActiveReservationTotal,
    bool IsBalanceConsistent,
    int FailedOrdersPendingCompensation);
