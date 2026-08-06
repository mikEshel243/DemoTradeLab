namespace DemoTradeLab.Core.DemoProfiles;

public sealed record DemoProfileValidationError(
    string PropertyName,
    string Message);
