namespace DemoTradeLab.Core.Trades;

public sealed class Trade
{
    private const int MaximumInstrumentLength = 100;

    private Trade(
        Guid id,
        string instrument,
        TradeDirection direction,
        DateTimeOffset openedAtUtc,
        DateTimeOffset closedAtUtc,
        decimal openingPrice,
        decimal closingPrice,
        decimal quantity,
        decimal realizedProfitLoss,
        string currency,
        decimal? fees,
        decimal? financingCosts,
        TradeDataSource source,
        DateTimeOffset? importedAtUtc)
    {
        Id = id;
        Instrument = instrument;
        Direction = direction;
        OpenedAtUtc = openedAtUtc;
        ClosedAtUtc = closedAtUtc;
        OpeningPrice = openingPrice;
        ClosingPrice = closingPrice;
        Quantity = quantity;
        RealizedProfitLoss = realizedProfitLoss;
        Currency = currency;
        Fees = fees;
        FinancingCosts = financingCosts;
        Source = source;
        ImportedAtUtc = importedAtUtc;
    }

    public Guid Id { get; }

    public string Instrument { get; }

    public TradeDirection Direction { get; }

    public DateTimeOffset OpenedAtUtc { get; }

    public DateTimeOffset ClosedAtUtc { get; }

    public decimal OpeningPrice { get; }

    public decimal ClosingPrice { get; }

    public decimal Quantity { get; }

    public decimal RealizedProfitLoss { get; }

    public string Currency { get; }

    public decimal? Fees { get; }

    public decimal? FinancingCosts { get; }

    public TradeDataSource Source { get; }

    public DateTimeOffset? ImportedAtUtc { get; }

    public static TradeCreationResult Create(TradeDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = new List<TradeValidationError>();
        var instrument = draft.Instrument?.Trim() ?? string.Empty;
        var currency = draft.Currency?.Trim().ToUpperInvariant() ?? string.Empty;

        ValidateInstrument(instrument, errors);
        ValidateDirection(draft.Direction, errors);
        ValidateTimestamps(draft, errors);
        ValidateFinancialValues(draft, errors);
        ValidateCurrency(currency, errors);
        ValidateSource(draft, errors);

        if (errors.Count > 0)
        {
            return TradeCreationResult.Failure(errors);
        }

        var trade = new Trade(
            Guid.NewGuid(),
            instrument,
            draft.Direction,
            draft.OpenedAtUtc,
            draft.ClosedAtUtc,
            draft.OpeningPrice,
            draft.ClosingPrice,
            draft.Quantity,
            draft.RealizedProfitLoss,
            currency,
            draft.Fees,
            draft.FinancingCosts,
            draft.Source,
            draft.ImportedAtUtc);

        return TradeCreationResult.Success(trade);
    }

    private static void ValidateInstrument(
        string instrument,
        ICollection<TradeValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(instrument))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.Instrument),
                TradeValidationCode.Required,
                "Instrument is required."));
        }
        else if (instrument.Length > MaximumInstrumentLength)
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.Instrument),
                TradeValidationCode.InvalidValue,
                $"Instrument must not exceed {MaximumInstrumentLength} characters."));
        }
    }

    private static void ValidateDirection(
        TradeDirection direction,
        ICollection<TradeValidationError> errors)
    {
        if (!Enum.IsDefined(direction))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.Direction),
                TradeValidationCode.InvalidValue,
                "Direction must be Buy or Sell."));
        }
    }

    private static void ValidateTimestamps(
        TradeDraft draft,
        ICollection<TradeValidationError> errors)
    {
        if (!IsUtc(draft.OpenedAtUtc))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.OpenedAtUtc),
                TradeValidationCode.MustBeUtc,
                "Opening timestamp must use the UTC offset."));
        }

        if (!IsUtc(draft.ClosedAtUtc))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.ClosedAtUtc),
                TradeValidationCode.MustBeUtc,
                "Closing timestamp must use the UTC offset."));
        }

        if (draft.ClosedAtUtc <= draft.OpenedAtUtc)
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.ClosedAtUtc),
                TradeValidationCode.InvalidTimeRange,
                "Closing timestamp must be later than the opening timestamp."));
        }
    }

    private static void ValidateFinancialValues(
        TradeDraft draft,
        ICollection<TradeValidationError> errors)
    {
        AddPositiveValueError(draft.OpeningPrice, nameof(TradeDraft.OpeningPrice), errors);
        AddPositiveValueError(draft.ClosingPrice, nameof(TradeDraft.ClosingPrice), errors);
        AddPositiveValueError(draft.Quantity, nameof(TradeDraft.Quantity), errors);

        AddNonNegativeValueError(draft.Fees, nameof(TradeDraft.Fees), errors);
        AddNonNegativeValueError(draft.FinancingCosts, nameof(TradeDraft.FinancingCosts), errors);
    }

    private static void ValidateCurrency(
        string currency,
        ICollection<TradeValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.Currency),
                TradeValidationCode.Required,
                "Currency is required."));
            return;
        }

        if (currency.Length != 3 || currency.Any(character => character is < 'A' or > 'Z'))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.Currency),
                TradeValidationCode.InvalidValue,
                "Currency must contain exactly three letters."));
        }
    }

    private static void ValidateSource(
        TradeDraft draft,
        ICollection<TradeValidationError> errors)
    {
        if (!Enum.IsDefined(draft.Source))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.Source),
                TradeValidationCode.InvalidValue,
                "Data source is not supported."));
        }

        if (draft.Source == TradeDataSource.Imported && draft.ImportedAtUtc is null)
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.ImportedAtUtc),
                TradeValidationCode.Required,
                "Import timestamp is required for an imported trade."));
            return;
        }

        if (draft.Source != TradeDataSource.Imported && draft.ImportedAtUtc is not null)
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.ImportedAtUtc),
                TradeValidationCode.NotApplicable,
                "Import timestamp is only valid for an imported trade."));
        }

        if (draft.ImportedAtUtc is not { } importedAtUtc)
        {
            return;
        }

        if (!IsUtc(importedAtUtc))
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.ImportedAtUtc),
                TradeValidationCode.MustBeUtc,
                "Import timestamp must use the UTC offset."));
        }

        if (importedAtUtc < draft.ClosedAtUtc)
        {
            errors.Add(new TradeValidationError(
                nameof(TradeDraft.ImportedAtUtc),
                TradeValidationCode.InvalidTimeRange,
                "Import timestamp cannot be earlier than the closing timestamp."));
        }
    }

    private static void AddPositiveValueError(
        decimal value,
        string propertyName,
        ICollection<TradeValidationError> errors)
    {
        if (value <= 0)
        {
            errors.Add(new TradeValidationError(
                propertyName,
                TradeValidationCode.InvalidValue,
                $"{propertyName} must be greater than zero."));
        }
    }

    private static void AddNonNegativeValueError(
        decimal? value,
        string propertyName,
        ICollection<TradeValidationError> errors)
    {
        if (value < 0)
        {
            errors.Add(new TradeValidationError(
                propertyName,
                TradeValidationCode.InvalidValue,
                $"{propertyName} cannot be negative."));
        }
    }

    private static bool IsUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero;
}
