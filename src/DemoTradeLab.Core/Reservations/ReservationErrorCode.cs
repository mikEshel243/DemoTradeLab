namespace DemoTradeLab.Core.Reservations;

public enum ReservationErrorCode
{
    InvalidAmount = 1,
    InsufficientFunds = 2,
    AccountNotFound = 3,
    ReservationNotFound = 4,
    ReservationNotActive = 5,
    AccountMismatch = 6,
    TimestampMustBeUtc = 7,
    TimestampBeforeCreation = 8,
    BalanceInvariantViolation = 9,
    IdempotencyKeyRequired = 10,
    IdempotencyKeyTooLong = 11,
    IdempotencyConflict = 12
}
