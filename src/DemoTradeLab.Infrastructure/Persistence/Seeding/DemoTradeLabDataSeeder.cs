using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Trades;
using Microsoft.EntityFrameworkCore;

namespace DemoTradeLab.Infrastructure.Persistence.Seeding;

internal static class DemoTradeLabDataSeeder
{
    public static void Seed(
        DbContext context,
        IReadOnlyList<DemoProfileSeed> demoProfileSeeds)
    {
        var hasChanges = false;

        if (!context.Set<Trade>().Any())
        {
            context.Set<Trade>().AddRange(CreateTrades());
            hasChanges = true;
        }

        var profiles = context.Set<DemoProfile>()
            .Include(profile => profile.Accounts)
            .ToList();

        hasChanges |= AddMissingDemoProfiles(
            context,
            profiles,
            demoProfileSeeds);

        if (hasChanges)
        {
            context.SaveChanges();
        }
    }

    public static async Task SeedAsync(
        DbContext context,
        IReadOnlyList<DemoProfileSeed> demoProfileSeeds,
        CancellationToken cancellationToken)
    {
        var hasChanges = false;

        if (!await context.Set<Trade>().AnyAsync(cancellationToken))
        {
            context.Set<Trade>().AddRange(CreateTrades());
            hasChanges = true;
        }

        var profiles = await context.Set<DemoProfile>()
            .Include(profile => profile.Accounts)
            .ToListAsync(cancellationToken);

        hasChanges |= AddMissingDemoProfiles(
            context,
            profiles,
            demoProfileSeeds);

        if (hasChanges)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool AddMissingDemoProfiles(
        DbContext context,
        ICollection<DemoProfile> persistedProfiles,
        IReadOnlyList<DemoProfileSeed> seedDefinitions)
    {
        var hasChanges = false;

        foreach (var seed in seedDefinitions)
        {
            var configuredProfile = CreateProfile(seed.Profile);
            var persistedProfile = persistedProfiles.SingleOrDefault(
                profile => profile.Key == configuredProfile.Key);

            if (persistedProfile is null)
            {
                persistedProfile = configuredProfile;
                persistedProfiles.Add(persistedProfile);
                context.Set<DemoProfile>().Add(persistedProfile);
                hasChanges = true;
            }

            foreach (var accountDraft in seed.Accounts)
            {
                var configuredAccount = AddAccount(configuredProfile, accountDraft);

                if (persistedProfile.Accounts.Any(
                        account => account.Key == configuredAccount.Key))
                {
                    continue;
                }

                AddAccount(persistedProfile, accountDraft);
                hasChanges = true;
            }
        }

        return hasChanges;
    }

    private static IReadOnlyList<Trade> CreateTrades() =>
        SampleTradeData.CreateDrafts()
            .Select(CreateTrade)
            .ToArray();

    private static Trade CreateTrade(TradeDraft draft)
    {
        var result = Trade.Create(draft);

        if (result.Trade is { } trade)
        {
            return trade;
        }

        var errorSummary = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.PropertyName}: {error.Message}"));

        throw new InvalidOperationException(
            $"Fictional sample trade configuration is invalid. {errorSummary}");
    }

    private static DemoProfile CreateProfile(DemoProfileDraft draft)
    {
        var result = DemoProfile.Create(draft);

        if (result.Profile is { } profile)
        {
            return profile;
        }

        throw CreateConfigurationException("profile", result.Errors);
    }

    private static DemoAccount AddAccount(
        DemoProfile profile,
        DemoAccountDraft draft)
    {
        var result = profile.AddAccount(draft);

        if (result.Account is { } account)
        {
            return account;
        }

        throw CreateConfigurationException("account", result.Errors);
    }

    private static InvalidOperationException CreateConfigurationException(
        string valueType,
        IEnumerable<DemoProfileValidationError> errors)
    {
        var errorSummary = string.Join(
            "; ",
            errors.Select(error => $"{error.PropertyName}: {error.Message}"));

        return new InvalidOperationException(
            $"Fictional demo {valueType} configuration is invalid. {errorSummary}");
    }
}
