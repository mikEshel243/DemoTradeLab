using DemoTradeLab.Core.Reservations;

namespace DemoTradeLab.Core.DemoProfiles;

public sealed class DemoAccount
{
    private const int MaximumKeyLength = 50;
    private const int MaximumDisplayNameLength = 100;

    private DemoAccount(
        Guid id,
        Guid demoProfileId,
        string key,
        string displayName,
        decimal totalBalance,
        decimal reservedBalance,
        string currency)
    {
        Id = id;
        DemoProfileId = demoProfileId;
        Key = key;
        DisplayName = displayName;
        TotalBalance = totalBalance;
        ReservedBalance = reservedBalance;
        Currency = currency;
    }

    public Guid Id { get; private set; }

    public Guid DemoProfileId { get; private set; }

    public string Key { get; private set; }

    public string DisplayName { get; private set; }

    public decimal TotalBalance { get; private set; }

    public decimal ReservedBalance { get; private set; }

    public decimal AvailableBalance => TotalBalance - ReservedBalance;

    public string Currency { get; private set; }

    internal static DemoAccountCreationResult Create(
        Guid demoProfileId,
        DemoAccountDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var key = NormalizeKey(draft.Key);
        var displayName = draft.DisplayName?.Trim() ?? string.Empty;
        var currency = draft.Currency?.Trim().ToUpperInvariant() ?? string.Empty;
        var errors = Validate(draft, key, displayName, currency);

        if (errors.Count > 0)
        {
            return DemoAccountCreationResult.Failure(errors);
        }

        return DemoAccountCreationResult.Success(new DemoAccount(
            Guid.NewGuid(),
            demoProfileId,
            key,
            displayName,
            draft.InitialBalance,
            0m,
            currency));
    }

    internal static string NormalizeKey(string? key) =>
        key?.Trim().ToLowerInvariant() ?? string.Empty;

    internal ReservationError? Reserve(decimal amount)
    {
        if (amount <= 0m)
        {
            return InvalidAmount();
        }

        if (amount > AvailableBalance)
        {
            return new ReservationError(
                nameof(AvailableBalance),
                ReservationErrorCode.InsufficientFunds,
                $"Available balance is {AvailableBalance} {Currency}; requested {amount} {Currency}.");
        }

        ReservedBalance += amount;
        return null;
    }

    internal ReservationError? Release(decimal amount)
    {
        var error = ValidateReservedAmount(amount);

        if (error is not null)
        {
            return error;
        }

        ReservedBalance -= amount;
        return null;
    }

    internal ReservationError? Consume(decimal amount)
    {
        var error = ValidateReservedAmount(amount);

        if (error is not null)
        {
            return error;
        }

        ReservedBalance -= amount;
        TotalBalance -= amount;
        return null;
    }

    private ReservationError? ValidateReservedAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            return InvalidAmount();
        }

        if (amount > ReservedBalance || amount > TotalBalance)
        {
            return new ReservationError(
                nameof(ReservedBalance),
                ReservationErrorCode.BalanceInvariantViolation,
                "The operation would make the persisted account balance invalid.");
        }

        return null;
    }

    private static ReservationError InvalidAmount() => new(
        nameof(DemoReservation.Amount),
        ReservationErrorCode.InvalidAmount,
        "Reservation amount must be greater than zero.");

    private static List<DemoProfileValidationError> Validate(
        DemoAccountDraft draft,
        string key,
        string displayName,
        string currency)
    {
        var errors = new List<DemoProfileValidationError>();

        AddKeyErrors(key, errors);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors.Add(new(nameof(DemoAccountDraft.DisplayName), "Display name is required."));
        }
        else if (displayName.Length > MaximumDisplayNameLength)
        {
            errors.Add(new(
                nameof(DemoAccountDraft.DisplayName),
                $"Display name must not exceed {MaximumDisplayNameLength} characters."));
        }

        if (draft.InitialBalance <= 0m)
        {
            errors.Add(new(
                nameof(DemoAccountDraft.InitialBalance),
                "Initial balance must be greater than zero."));
        }

        if (currency.Length != 3 || currency.Any(character => character is < 'A' or > 'Z'))
        {
            errors.Add(new(
                nameof(DemoAccountDraft.Currency),
                "Currency must contain exactly three letters."));
        }

        return errors;
    }

    private static void AddKeyErrors(
        string key,
        ICollection<DemoProfileValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(new(nameof(DemoAccountDraft.Key), "Key is required."));
        }
        else if (key.Length > MaximumKeyLength)
        {
            errors.Add(new(
                nameof(DemoAccountDraft.Key),
                $"Key must not exceed {MaximumKeyLength} characters."));
        }
        else if (key.Any(character =>
                     character is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))
        {
            errors.Add(new(
                nameof(DemoAccountDraft.Key),
                "Key may contain only lowercase letters, numbers, and hyphens."));
        }
    }
}
