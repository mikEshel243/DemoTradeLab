using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.Api.Contracts.Reservations;

internal static class ReservationContractMapper
{
    public static ReservationResponse ToResponse(this DemoReservation reservation) =>
        new(
            reservation.Id,
            reservation.DemoAccountId,
            reservation.Amount,
            reservation.Currency,
            reservation.Status,
            reservation.CreatedAtUtc,
            reservation.CompletedAtUtc);
}
