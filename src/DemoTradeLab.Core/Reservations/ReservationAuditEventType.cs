namespace DemoTradeLab.Core.Reservations;

public enum ReservationAuditEventType
{
    Created = 1,
    RejectedInsufficientFunds = 2,
    Released = 3,
    Consumed = 4
}
