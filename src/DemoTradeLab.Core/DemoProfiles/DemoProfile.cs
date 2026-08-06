namespace DemoTradeLab.Core.DemoProfiles;

public sealed class DemoProfile
{
    private const int MaximumKeyLength = 50;
    private const int MaximumDisplayNameLength = 100;
    private readonly List<DemoAccount> _accounts = [];

    private DemoProfile(Guid id, string key, string displayName)
    {
        Id = id;
        Key = key;
        DisplayName = displayName;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; }

    public string DisplayName { get; private set; }

    public IReadOnlyCollection<DemoAccount> Accounts => _accounts.AsReadOnly();

    public static DemoProfileCreationResult Create(DemoProfileDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var key = NormalizeKey(draft.Key);
        var displayName = draft.DisplayName?.Trim() ?? string.Empty;
        var errors = Validate(key, displayName);

        if (errors.Count > 0)
        {
            return DemoProfileCreationResult.Failure(errors);
        }

        return DemoProfileCreationResult.Success(
            new DemoProfile(Guid.NewGuid(), key, displayName));
    }

    public DemoAccountCreationResult AddAccount(DemoAccountDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var normalizedKey = DemoAccount.NormalizeKey(draft.Key);

        if (_accounts.Any(account => account.Key == normalizedKey))
        {
            return DemoAccountCreationResult.Failure(
            [
                new DemoProfileValidationError(
                    nameof(DemoAccountDraft.Key),
                    $"An account with key '{normalizedKey}' already exists in this profile.")
            ]);
        }

        var result = DemoAccount.Create(Id, draft);

        if (result.Account is { } account)
        {
            _accounts.Add(account);
        }

        return result;
    }

    internal static string NormalizeKey(string? key) =>
        key?.Trim().ToLowerInvariant() ?? string.Empty;

    private static List<DemoProfileValidationError> Validate(
        string key,
        string displayName)
    {
        var errors = new List<DemoProfileValidationError>();

        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(new(nameof(DemoProfileDraft.Key), "Key is required."));
        }
        else if (key.Length > MaximumKeyLength)
        {
            errors.Add(new(
                nameof(DemoProfileDraft.Key),
                $"Key must not exceed {MaximumKeyLength} characters."));
        }
        else if (key.Any(character =>
                     character is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))
        {
            errors.Add(new(
                nameof(DemoProfileDraft.Key),
                "Key may contain only lowercase letters, numbers, and hyphens."));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors.Add(new(nameof(DemoProfileDraft.DisplayName), "Display name is required."));
        }
        else if (displayName.Length > MaximumDisplayNameLength)
        {
            errors.Add(new(
                nameof(DemoProfileDraft.DisplayName),
                $"Display name must not exceed {MaximumDisplayNameLength} characters."));
        }

        return errors;
    }
}
