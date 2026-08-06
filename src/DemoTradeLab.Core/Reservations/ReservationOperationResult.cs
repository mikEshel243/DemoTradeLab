using System.Diagnostics.CodeAnalysis;

namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationOperationResult
{
    private ReservationOperationResult(
        DemoReservation? reservation,
        IReadOnlyList<ReservationError> errors)
    {
        Reservation = reservation;
        Errors = errors;
    }

    [MemberNotNullWhen(true, nameof(Reservation))]
    public bool IsSuccess => Reservation is not null;

    public DemoReservation? Reservation { get; }

    public IReadOnlyList<ReservationError> Errors { get; }

    internal static ReservationOperationResult Success(DemoReservation reservation) =>
        new(reservation, Array.Empty<ReservationError>());

    internal static ReservationOperationResult Failure(params ReservationError[] errors) =>
        new(null, errors);

    internal static ReservationOperationResult Failure(IEnumerable<ReservationError> errors) =>
        new(null, errors.ToArray());
}
