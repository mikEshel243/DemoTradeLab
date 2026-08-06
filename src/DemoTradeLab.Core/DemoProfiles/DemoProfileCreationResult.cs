using System.Diagnostics.CodeAnalysis;

namespace DemoTradeLab.Core.DemoProfiles;

public sealed class DemoProfileCreationResult
{
    private DemoProfileCreationResult(
        DemoProfile? profile,
        IReadOnlyList<DemoProfileValidationError> errors)
    {
        Profile = profile;
        Errors = errors;
    }

    [MemberNotNullWhen(true, nameof(Profile))]
    public bool IsSuccess => Profile is not null;

    public DemoProfile? Profile { get; }

    public IReadOnlyList<DemoProfileValidationError> Errors { get; }

    internal static DemoProfileCreationResult Success(DemoProfile profile) =>
        new(profile, Array.Empty<DemoProfileValidationError>());

    internal static DemoProfileCreationResult Failure(
        IEnumerable<DemoProfileValidationError> errors) =>
        new(null, errors.ToArray());
}
