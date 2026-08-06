using DemoTradeLab.Core.DemoProfiles;

namespace DemoTradeLab.Api.Contracts.DemoProfiles;

internal static class DemoProfileContractMapper
{
    public static DemoProfileResponse ToResponse(this DemoProfile profile) =>
        new(
            profile.Id,
            profile.Key,
            profile.DisplayName,
            profile.Accounts
                .OrderBy(account => account.Key)
                .Select(account => new DemoAccountResponse(
                    account.Id,
                    account.Key,
                    account.DisplayName,
                    account.TotalBalance,
                    account.ReservedBalance,
                    account.AvailableBalance,
                    account.Currency))
                .ToArray());
}
