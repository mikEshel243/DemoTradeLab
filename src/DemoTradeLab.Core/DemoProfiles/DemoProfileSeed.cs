namespace DemoTradeLab.Core.DemoProfiles;

public sealed record DemoProfileSeed(
    DemoProfileDraft Profile,
    IReadOnlyList<DemoAccountDraft> Accounts);
