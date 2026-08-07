using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.UnitTests.DemoProfiles;

public sealed class DemoProfileTests
{
    /// <summary>
    /// Creates a profile and account from valid drafts and verifies normalization of their keys and display values.
    /// </summary>
    [Fact]
    public void CreateAndAddAccount_WithValidDrafts_NormalizesValues()
    {
        var profileResult = DemoProfile.Create(new DemoProfileDraft(
            "  PRIMARY-DEMO ",
            " Primary Demo Profile "));

        var profile = Assert.IsType<DemoProfile>(profileResult.Profile);
        var accountResult = profile.AddAccount(new DemoAccountDraft(
            " MAIN-ACCOUNT ",
            " Main Account ",
            1_000m,
            " usd "));

        var account = Assert.IsType<DemoAccount>(accountResult.Account);
        Assert.Equal("primary-demo", profile.Key);
        Assert.Equal("Primary Demo Profile", profile.DisplayName);
        Assert.Equal("main-account", account.Key);
        Assert.Equal("Main Account", account.DisplayName);
        Assert.Equal("USD", account.Currency);
        Assert.Equal(1_000m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(1_000m, account.AvailableBalance);
    }

    /// <summary>
    /// Supplies an invalid profile key and verifies that the domain returns a validation error instead of an entity.
    /// </summary>
    [Fact]
    public void Create_WithInvalidKey_ReturnsValidationError()
    {
        var result = DemoProfile.Create(new DemoProfileDraft(
            "not valid!",
            "Demo Profile"));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(DemoProfileDraft.Key));
    }

    /// <summary>
    /// Adds keys that normalize to the same value and verifies that duplicate accounts cannot enter one profile.
    /// </summary>
    [Fact]
    public void AddAccount_WithDuplicateNormalizedKey_ReturnsValidationError()
    {
        var profile = Assert.IsType<DemoProfile>(
            DemoProfile.Create(new DemoProfileDraft("demo", "Demo")).Profile);
        profile.AddAccount(new DemoAccountDraft(
            "main-account",
            "Main Account",
            100m,
            "USD"));

        var duplicateResult = profile.AddAccount(new DemoAccountDraft(
            " MAIN-ACCOUNT ",
            "Another Account",
            200m,
            "USD"));

        Assert.False(duplicateResult.IsSuccess);
        Assert.Single(profile.Accounts);
    }

    /// <summary>
    /// Attempts to add an account with a non-positive initial balance and verifies that the balance invariant is enforced.
    /// </summary>
    [Fact]
    public void AddAccount_WithNonPositiveBalance_ReturnsValidationError()
    {
        var profile = Assert.IsType<DemoProfile>(
            DemoProfile.Create(new DemoProfileDraft("demo", "Demo")).Profile);

        var result = profile.AddAccount(new DemoAccountDraft(
            "main-account",
            "Main Account",
            0m,
            "USD"));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(DemoAccountDraft.InitialBalance));
        Assert.Empty(profile.Accounts);
    }
}
