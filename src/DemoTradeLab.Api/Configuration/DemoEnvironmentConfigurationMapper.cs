using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Api.Configuration;

internal static class DemoEnvironmentConfigurationMapper
{
    public static IReadOnlyList<DemoProfileSeed> ToSeedDefinitions(
        this DemoEnvironmentOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Profiles
            .Select(profile => new DemoProfileSeed(
                new DemoProfileDraft(profile.Key, profile.DisplayName),
                profile.Accounts
                    .Select(account => new DemoAccountDraft(
                        account.Key,
                        account.DisplayName,
                        account.InitialBalance,
                        account.Currency))
                    .ToArray()))
            .ToArray();
    }
}
