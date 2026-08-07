using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.UnitTests.Reservations;

public sealed class DemoReservationTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(
        2026,
        8,
        6,
        10,
        0,
        0,
        TimeSpan.Zero);

    /// <summary>
    /// Reserves an affordable amount and verifies creation of an active reservation and the matching balance change.
    /// </summary>
    [Fact]
    public void Create_WithAvailableFunds_ReservesBalanceAndCreatesActiveReservation()
    {
        var account = CreateAccount(100m);

        var result = DemoReservation.Create(account, 80m, CreatedAtUtc);

        var reservation = Assert.IsType<DemoReservation>(result.Reservation);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(80m, reservation.Amount);
        Assert.Equal("USD", reservation.Currency);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(80m, account.ReservedBalance);
        Assert.Equal(20m, account.AvailableBalance);
    }

    /// <summary>
    /// Requests more than the available balance and verifies an expected rejection with no account mutation.
    /// </summary>
    [Fact]
    public void Create_WithInsufficientFunds_ReturnsRejectionWithoutChangingBalance()
    {
        var account = CreateAccount(100m);

        var result = DemoReservation.Create(account, 120m, CreatedAtUtc);

        Assert.False(result.IsSuccess);
        AssertHasError(result, ReservationErrorCode.InsufficientFunds);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(100m, account.AvailableBalance);
    }

    /// <summary>
    /// Supplies an invalid amount and timestamp and verifies that all validation problems are reported together.
    /// </summary>
    [Fact]
    public void Create_WithInvalidAmountAndTimestamp_ReturnsAllValidationErrors()
    {
        var account = CreateAccount(100m);
        var nonUtcTime = CreatedAtUtc.ToOffset(TimeSpan.FromHours(3));

        var result = DemoReservation.Create(account, 0m, nonUtcTime);

        Assert.False(result.IsSuccess);
        AssertHasError(result, ReservationErrorCode.InvalidAmount);
        AssertHasError(result, ReservationErrorCode.TimestampMustBeUtc);
        Assert.Equal(0m, account.ReservedBalance);
    }

    /// <summary>
    /// Releases an active reservation and verifies that reserved funds become available without changing total balance.
    /// </summary>
    [Fact]
    public void Release_ActiveReservation_RestoresAvailableBalance()
    {
        var account = CreateAccount(100m);
        var reservation = CreateReservation(account, 80m);
        var completedAtUtc = CreatedAtUtc.AddMinutes(1);

        var result = reservation.Release(account, completedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(completedAtUtc, reservation.CompletedAtUtc);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(100m, account.AvailableBalance);
    }

    /// <summary>
    /// Consumes an active reservation and verifies equal reductions to total and reserved balances.
    /// </summary>
    [Fact]
    public void Consume_ActiveReservation_ReducesTotalAndReservedBalances()
    {
        var account = CreateAccount(100m);
        var reservation = CreateReservation(account, 80m);

        var result = reservation.Consume(account, CreatedAtUtc.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(ReservationStatus.Consumed, reservation.Status);
        Assert.Equal(20m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(20m, account.AvailableBalance);
    }

    /// <summary>
    /// Attempts to release a terminal reservation and verifies a rejection without a second balance change.
    /// </summary>
    [Fact]
    public void Release_CompletedReservation_ReturnsRejectionWithoutChangingBalance()
    {
        var account = CreateAccount(100m);
        var reservation = CreateReservation(account, 80m);
        reservation.Release(account, CreatedAtUtc.AddMinutes(1));

        var secondResult = reservation.Release(account, CreatedAtUtc.AddMinutes(2));

        AssertHasError(secondResult, ReservationErrorCode.ReservationNotActive);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
    }

    /// <summary>
    /// Uses an account other than the reservation owner and verifies rejection without mutating either account.
    /// </summary>
    [Fact]
    public void Release_WithDifferentAccount_ReturnsRejectionWithoutChangingEitherAccount()
    {
        var owningAccount = CreateAccount(100m, "owner");
        var otherAccount = CreateAccount(100m, "other");
        var reservation = CreateReservation(owningAccount, 80m);

        var result = reservation.Release(otherAccount, CreatedAtUtc.AddMinutes(1));

        AssertHasError(result, ReservationErrorCode.AccountMismatch);
        Assert.Equal(80m, owningAccount.ReservedBalance);
        Assert.Equal(0m, otherAccount.ReservedBalance);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
    }

    /// <summary>
    /// Attempts completion before the reservation creation time and verifies rejection without changing persisted state values.
    /// </summary>
    [Fact]
    public void Consume_WithTimestampBeforeCreation_ReturnsRejectionWithoutChangingState()
    {
        var account = CreateAccount(100m);
        var reservation = CreateReservation(account, 80m);

        var result = reservation.Consume(account, CreatedAtUtc.AddSeconds(-1));

        AssertHasError(result, ReservationErrorCode.TimestampBeforeCreation);
        Assert.Equal(100m, account.TotalBalance);
        Assert.Equal(80m, account.ReservedBalance);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
    }

    private static DemoAccount CreateAccount(
        decimal initialBalance,
        string accountKey = "main-account")
    {
        var profile = Assert.IsType<DemoProfile>(DemoProfile.Create(
            new DemoProfileDraft($"{accountKey}-profile", "Demo Profile")).Profile);
        var result = profile.AddAccount(new DemoAccountDraft(
            accountKey,
            "Demo Account",
            initialBalance,
            "USD"));

        return Assert.IsType<DemoAccount>(result.Account);
    }

    private static DemoReservation CreateReservation(DemoAccount account, decimal amount) =>
        Assert.IsType<DemoReservation>(
            DemoReservation.Create(account, amount, CreatedAtUtc).Reservation);

    private static void AssertHasError(
        ReservationOperationResult result,
        ReservationErrorCode code)
    {
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == code);
    }
}
