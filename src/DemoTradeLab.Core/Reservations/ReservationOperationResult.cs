using System.Diagnostics.CodeAnalysis;

namespace DemoTradeLab.Core.Reservations;

public sealed class ReservationOperationResult
{
    private ReservationOperationResult(
        DemoReservation? reservation,
        IReadOnlyList<ReservationError> errors,
        bool isReplay)
    {
        Reservation = reservation;
        Errors = errors;
        IsReplay = isReplay;
    }

    [MemberNotNullWhen(true, nameof(Reservation))]
    public bool IsSuccess => Reservation is not null;

    public DemoReservation? Reservation { get; }

    public IReadOnlyList<ReservationError> Errors { get; }

    public bool IsReplay { get; }

    internal static ReservationOperationResult Success(DemoReservation reservation) =>
        new(reservation, Array.Empty<ReservationError>(), false);

    internal static ReservationOperationResult Failure(params ReservationError[] errors) =>
        new(null, errors, false);

    internal static ReservationOperationResult Failure(IEnumerable<ReservationError> errors) =>
        new(null, errors.ToArray(), false);

    internal ReservationOperationResult AsReplay() =>
        new(Reservation, Errors, true);
}
