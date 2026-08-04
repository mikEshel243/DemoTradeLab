using System.ComponentModel.DataAnnotations;
using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Api.Contracts.Trades;

public sealed class ListTradesQueryRequest : IValidatableObject
{
    public string? Instrument { get; init; }

    public string? Currency { get; init; }

    public TradeDirection? Direction { get; init; }

    public TradeDataSource? Source { get; init; }

    public TradeOutcome? Outcome { get; init; }

    public DateTimeOffset? ClosedFromUtc { get; init; }

    public DateTimeOffset? ClosedToUtc { get; init; }

    public TradeSortField SortBy { get; init; } = TradeSortField.ClosedAtUtc;

    public TradeSortDirection SortDirection { get; init; } = TradeSortDirection.Descending;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Instrument is not null)
        {
            var instrument = Instrument.Trim();

            if (instrument.Length == 0)
            {
                yield return new ValidationResult(
                    "Instrument cannot be blank when supplied.",
                    [nameof(Instrument)]);
            }
            else if (instrument.Length > 100)
            {
                yield return new ValidationResult(
                    "Instrument must not exceed 100 characters.",
                    [nameof(Instrument)]);
            }
        }

        if (Currency is not null)
        {
            var currency = Currency.Trim();

            if (currency.Length != 3 ||
                currency.Any(character => !char.IsAsciiLetter(character)))
            {
                yield return new ValidationResult(
                    "Currency must contain exactly three letters.",
                    [nameof(Currency)]);
            }
        }

        if (Direction is { } direction && !Enum.IsDefined(direction))
        {
            yield return UnsupportedEnum(nameof(Direction));
        }

        if (Source is { } source && !Enum.IsDefined(source))
        {
            yield return UnsupportedEnum(nameof(Source));
        }

        if (Outcome is { } outcome && !Enum.IsDefined(outcome))
        {
            yield return UnsupportedEnum(nameof(Outcome));
        }

        if (!Enum.IsDefined(SortBy))
        {
            yield return UnsupportedEnum(nameof(SortBy));
        }

        if (!Enum.IsDefined(SortDirection))
        {
            yield return UnsupportedEnum(nameof(SortDirection));
        }

        if (ClosedFromUtc is { } closedFromUtc && closedFromUtc.Offset != TimeSpan.Zero)
        {
            yield return MustBeUtc(nameof(ClosedFromUtc));
        }

        if (ClosedToUtc is { } closedToUtc && closedToUtc.Offset != TimeSpan.Zero)
        {
            yield return MustBeUtc(nameof(ClosedToUtc));
        }

        if (ClosedFromUtc is { } fromUtc &&
            ClosedToUtc is { } toUtc &&
            fromUtc > toUtc)
        {
            yield return new ValidationResult(
                "ClosedFromUtc cannot be later than ClosedToUtc.",
                [nameof(ClosedFromUtc), nameof(ClosedToUtc)]);
        }
    }

    public TradeListQuery ToQuery() =>
        new(
            Instrument,
            Currency,
            Direction,
            Source,
            Outcome,
            ClosedFromUtc,
            ClosedToUtc,
            SortBy,
            SortDirection);

    private static ValidationResult UnsupportedEnum(string propertyName) =>
        new(
            $"{propertyName} contains an unsupported value.",
            [propertyName]);

    private static ValidationResult MustBeUtc(string propertyName) =>
        new(
            $"{propertyName} must use the UTC offset.",
            [propertyName]);
}
