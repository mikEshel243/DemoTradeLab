using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Orders;
using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.UnitTests.Orders;

public sealed class DemoOrderTests
{
    private static readonly DateTimeOffset Now = new(
        2026,
        8,
        6,
        15,
        0,
        0,
        TimeSpan.Zero);

    /// <summary>
    /// Creates an order from an active reservation and verifies that it starts pending without changing reserved funds.
    /// </summary>
    [Fact]
    public void Create_FromActiveReservation_CreatesPendingOrderWithoutChangingBalance()
    {
        var (account, reservation) = CreateReservedAccount();

        var result = DemoOrder.Create(reservation, Now);

        var order = Assert.IsType<DemoOrder>(result.Order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(reservation.Id, order.ReservationId);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(80m, account.ReservedBalance);
    }

    /// <summary>
    /// Completes a pending order and verifies that its reservation and account balances are consumed together.
    /// </summary>
    [Fact]
    public void Complete_PendingOrder_ConsumesReservationAndBalance()
    {
        var (account, reservation) = CreateReservedAccount();
        var order = CreateOrder(reservation);

        var result = order.Complete(reservation, account, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(ReservationStatus.Consumed, reservation.Status);
        Assert.Equal(20m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(20m, account.AvailableBalance);
    }

    /// <summary>
    /// Fails an order and then compensates it, proving that failure recording and fund release are separate transitions.
    /// </summary>
    [Fact]
    public void FailThenCompensate_ReleasesReservationInSeparateTransition()
    {
        var (account, reservation) = CreateReservedAccount();
        var order = CreateOrder(reservation);

        var failed = order.MarkFailed(Now.AddMinutes(1));

        Assert.True(failed.IsSuccess);
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(80m, account.ReservedBalance);

        var compensated = order.Compensate(
            reservation,
            account,
            Now.AddMinutes(2));

        Assert.True(compensated.IsSuccess);
        Assert.Equal(OrderStatus.Compensated, order.Status);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
    }

    /// <summary>
    /// Attempts to complete a failed order and verifies a business rejection with no order or balance mutation.
    /// </summary>
    [Fact]
    public void Complete_FailedOrder_ReturnsBusinessRejectionWithoutMutation()
    {
        var (account, reservation) = CreateReservedAccount();
        var order = CreateOrder(reservation);
        order.MarkFailed(Now.AddMinutes(1));

        var result = order.Complete(reservation, account, Now.AddMinutes(2));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == OrderErrorCode.InvalidState);
        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(80m, account.ReservedBalance);
    }

    /// <summary>
    /// Repeats compensation after the target state was reached and verifies a successful retry-safe no-op.
    /// </summary>
    [Fact]
    public void Compensate_AlreadyCompensatedOrder_ReturnsSuccessfulNoOp()
    {
        var (account, reservation) = CreateReservedAccount();
        var order = CreateOrder(reservation);
        order.MarkFailed(Now.AddMinutes(1));
        order.Compensate(reservation, account, Now.AddMinutes(2));

        var retry = order.Compensate(reservation, account, Now.AddMinutes(3));

        Assert.True(retry.IsSuccess);
        Assert.True(retry.IsNoOp);
        Assert.Equal(0m, account.ReservedBalance);
    }

    private static (DemoAccount Account, DemoReservation Reservation)
        CreateReservedAccount()
    {
        var profile = Assert.IsType<DemoProfile>(DemoProfile.Create(
            new DemoProfileDraft("order-test", "Order Test")).Profile);
        var account = Assert.IsType<DemoAccount>(profile.AddAccount(new DemoAccountDraft(
            "main-account",
            "Main Account",
            100m,
            "USD")).Account);
        var reservation = Assert.IsType<DemoReservation>(DemoReservation.Create(
            account,
            80m,
            Now).Reservation);

        return (account, reservation);
    }

    private static DemoOrder CreateOrder(DemoReservation reservation) =>
        Assert.IsType<DemoOrder>(DemoOrder.Create(reservation, Now).Order);
}
