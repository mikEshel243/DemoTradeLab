namespace DemoTradeLab.Core.Reservations;

public sealed record ReservationError(
    string PropertyName,
    ReservationErrorCode Code,
    string Message);
