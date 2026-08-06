namespace DemoTradeLab.Core.Orders;

public enum OrderErrorCode
{
    AccountNotFound = 1,
    ReservationNotFound = 2,
    OrderNotFound = 3,
    ReservationNotActive = 4,
    InvalidState = 5,
    AccountMismatch = 6,
    InvalidTimestamp = 7
}
