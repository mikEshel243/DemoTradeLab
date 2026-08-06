using System.Diagnostics.CodeAnalysis;

namespace DemoTradeLab.Core.DemoProfiles;

public sealed class DemoAccountCreationResult
{
    private DemoAccountCreationResult(
        DemoAccount? account,
        IReadOnlyList<DemoProfileValidationError> errors)
    {
        Account = account;
        Errors = errors;
    }

    [MemberNotNullWhen(true, nameof(Account))]
    public bool IsSuccess => Account is not null;

    public DemoAccount? Account { get; }

    public IReadOnlyList<DemoProfileValidationError> Errors { get; }

    internal static DemoAccountCreationResult Success(DemoAccount account) =>
        new(account, Array.Empty<DemoProfileValidationError>());

    internal static DemoAccountCreationResult Failure(
        IEnumerable<DemoProfileValidationError> errors) =>
        new(null, errors.ToArray());
}
