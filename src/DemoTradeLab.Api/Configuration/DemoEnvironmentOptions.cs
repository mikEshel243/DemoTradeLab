using System.ComponentModel.DataAnnotations;
using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Api.Configuration;

public sealed class DemoEnvironmentOptions : IValidatableObject
{
    public const string SectionName = "DemoEnvironment";

    public List<DemoProfileOptions> Profiles { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Profiles.Count == 0)
        {
            yield return new ValidationResult(
                "At least one fictional demo profile must be configured.",
                [nameof(Profiles)]);
            yield break;
        }

        var profileKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var profileIndex = 0; profileIndex < Profiles.Count; profileIndex++)
        {
            var profileOptions = Profiles[profileIndex];
            var profilePath = $"{nameof(Profiles)}[{profileIndex}]";
            var profileResult = DemoProfile.Create(
                new DemoProfileDraft(profileOptions.Key, profileOptions.DisplayName));

            foreach (var error in profileResult.Errors)
            {
                yield return new ValidationResult(
                    error.Message,
                    [$"{profilePath}.{error.PropertyName}"]);
            }

            var normalizedProfileKey = NormalizeKey(profileOptions.Key);

            if (!profileKeys.Add(normalizedProfileKey))
            {
                yield return new ValidationResult(
                    $"Demo profile key '{normalizedProfileKey}' is configured more than once.",
                    [$"{profilePath}.{nameof(DemoProfileOptions.Key)}"]);
            }

            if (profileOptions.Accounts.Count == 0)
            {
                yield return new ValidationResult(
                    "Each demo profile must contain at least one fictional account.",
                    [$"{profilePath}.{nameof(DemoProfileOptions.Accounts)}"]);
                continue;
            }

            if (profileResult.Profile is not { } profile)
            {
                continue;
            }

            for (var accountIndex = 0;
                 accountIndex < profileOptions.Accounts.Count;
                 accountIndex++)
            {
                var accountOptions = profileOptions.Accounts[accountIndex];
                var accountPath =
                    $"{profilePath}.{nameof(DemoProfileOptions.Accounts)}[{accountIndex}]";
                var accountResult = profile.AddAccount(new DemoAccountDraft(
                    accountOptions.Key,
                    accountOptions.DisplayName,
                    accountOptions.InitialBalance,
                    accountOptions.Currency));

                foreach (var error in accountResult.Errors)
                {
                    yield return new ValidationResult(
                        error.Message,
                        [$"{accountPath}.{error.PropertyName}"]);
                }
            }
        }
    }

    private static string NormalizeKey(string? key) =>
        key?.Trim().ToLowerInvariant() ?? string.Empty;
}

public sealed class DemoProfileOptions
{
    public string? Key { get; init; }

    public string? DisplayName { get; init; }

    public List<DemoAccountOptions> Accounts { get; init; } = [];
}

public sealed class DemoAccountOptions
{
    public string? Key { get; init; }

    public string? DisplayName { get; init; }

    public decimal InitialBalance { get; init; }

    public string? Currency { get; init; }
}
