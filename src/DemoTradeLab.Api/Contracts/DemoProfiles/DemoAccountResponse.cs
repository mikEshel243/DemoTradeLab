namespace DemoTradeLab.Api.Contracts.DemoProfiles;

public sealed record DemoAccountResponse(
    Guid Id,
    string Key,
    string DisplayName,
    decimal TotalBalance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    string Currency);
