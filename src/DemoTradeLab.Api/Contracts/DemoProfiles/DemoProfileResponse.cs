namespace DemoTradeLab.Api.Contracts.DemoProfiles;

public sealed record DemoProfileResponse(
    Guid Id,
    string Key,
    string DisplayName,
    IReadOnlyList<DemoAccountResponse> Accounts);
