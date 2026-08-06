namespace DemoTradeLab.Core.DemoProfiles;

public sealed record DemoAccountDraft(
    string? Key,
    string? DisplayName,
    decimal InitialBalance,
    string? Currency);
