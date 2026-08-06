using System.ComponentModel.DataAnnotations;
using DemoTradeLab.Api.Configuration;

namespace DemoTradeLab.IntegrationTests.Configuration;

public sealed class DemoEnvironmentOptionsTests
{
    [Fact]
    public void Validate_WithDuplicateProfileAndAccountKeys_ReturnsErrors()
    {
        var options = new DemoEnvironmentOptions
        {
            Profiles =
            [
                new DemoProfileOptions
                {
                    Key = "demo",
                    DisplayName = "Demo One",
                    Accounts =
                    [
                        CreateAccount("main-account"),
                        CreateAccount(" MAIN-ACCOUNT ")
                    ]
                },
                new DemoProfileOptions
                {
                    Key = " DEMO ",
                    DisplayName = "Demo Two",
                    Accounts = [CreateAccount("secondary-account")]
                }
            ]
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(
            validationResults,
            result => result.ErrorMessage?.Contains("account", StringComparison.OrdinalIgnoreCase)
                == true);
        Assert.Contains(
            validationResults,
            result => result.ErrorMessage?.Contains("profile", StringComparison.OrdinalIgnoreCase)
                == true);
    }

    private static DemoAccountOptions CreateAccount(string key) => new()
    {
        Key = key,
        DisplayName = "Demo Account",
        InitialBalance = 100m,
        Currency = "USD"
    };
}
