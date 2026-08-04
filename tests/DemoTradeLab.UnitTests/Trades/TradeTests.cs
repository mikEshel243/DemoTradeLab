using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.UnitTests.Trades;

public sealed class TradeTests
{
    [Fact]
    public void Create_WithValidDraft_CreatesNormalizedTrade()
    {
        var draft = CreateValidDraft() with
        {
            Instrument = "  EUR/USD  ",
            Currency = " usd ",
            RealizedProfitLoss = -25.40m
        };

        var result = Trade.Create(draft);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        var trade = Assert.IsType<Trade>(result.Trade);
        Assert.NotEqual(Guid.Empty, trade.Id);
        Assert.Equal("EUR/USD", trade.Instrument);
        Assert.Equal("USD", trade.Currency);
        Assert.Equal(-25.40m, trade.RealizedProfitLoss);
    }

    [Fact]
    public void Create_WithMissingTextAndUnknownEnums_ReturnsValidationErrors()
    {
        var draft = CreateValidDraft() with
        {
            Instrument = " ",
            Currency = "US1",
            Direction = (TradeDirection)999,
            Source = (TradeDataSource)999
        };

        var result = Trade.Create(draft);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Trade);
        AssertHasError(result, nameof(TradeDraft.Instrument), TradeValidationCode.Required);
        AssertHasError(result, nameof(TradeDraft.Currency), TradeValidationCode.InvalidValue);
        AssertHasError(result, nameof(TradeDraft.Direction), TradeValidationCode.InvalidValue);
        AssertHasError(result, nameof(TradeDraft.Source), TradeValidationCode.InvalidValue);
    }

    [Fact]
    public void Create_WithInvalidFinancialValues_ReturnsAllValidationErrors()
    {
        var draft = CreateValidDraft() with
        {
            OpeningPrice = 0m,
            ClosingPrice = -1m,
            Quantity = 0m,
            Fees = -0.01m,
            FinancingCosts = -0.01m
        };

        var result = Trade.Create(draft);

        Assert.False(result.IsSuccess);
        AssertHasError(result, nameof(TradeDraft.OpeningPrice), TradeValidationCode.InvalidValue);
        AssertHasError(result, nameof(TradeDraft.ClosingPrice), TradeValidationCode.InvalidValue);
        AssertHasError(result, nameof(TradeDraft.Quantity), TradeValidationCode.InvalidValue);
        AssertHasError(result, nameof(TradeDraft.Fees), TradeValidationCode.InvalidValue);
        AssertHasError(result, nameof(TradeDraft.FinancingCosts), TradeValidationCode.InvalidValue);
    }

    [Fact]
    public void Create_WithNonUtcTimestamps_ReturnsValidationErrors()
    {
        var localOffset = TimeSpan.FromHours(2);
        var draft = CreateValidDraft() with
        {
            OpenedAtUtc = new DateTimeOffset(2026, 8, 4, 10, 0, 0, localOffset),
            ClosedAtUtc = new DateTimeOffset(2026, 8, 4, 10, 30, 0, localOffset)
        };

        var result = Trade.Create(draft);

        Assert.False(result.IsSuccess);
        AssertHasError(result, nameof(TradeDraft.OpenedAtUtc), TradeValidationCode.MustBeUtc);
        AssertHasError(result, nameof(TradeDraft.ClosedAtUtc), TradeValidationCode.MustBeUtc);
    }

    [Fact]
    public void Create_WhenClosingTimeIsNotAfterOpeningTime_ReturnsValidationError()
    {
        var draft = CreateValidDraft() with
        {
            ClosedAtUtc = CreateValidDraft().OpenedAtUtc
        };

        var result = Trade.Create(draft);

        Assert.False(result.IsSuccess);
        AssertHasError(result, nameof(TradeDraft.ClosedAtUtc), TradeValidationCode.InvalidTimeRange);
    }

    [Fact]
    public void Create_WhenImportedTradeHasNoImportTimestamp_ReturnsValidationError()
    {
        var draft = CreateValidDraft() with
        {
            Source = TradeDataSource.Imported,
            ImportedAtUtc = null
        };

        var result = Trade.Create(draft);

        Assert.False(result.IsSuccess);
        AssertHasError(result, nameof(TradeDraft.ImportedAtUtc), TradeValidationCode.Required);
    }

    [Fact]
    public void Create_WithValidImportedTrade_CreatesTrade()
    {
        var draft = CreateValidDraft() with
        {
            Source = TradeDataSource.Imported,
            ImportedAtUtc = new DateTimeOffset(2026, 8, 4, 11, 0, 0, TimeSpan.Zero)
        };

        var result = Trade.Create(draft);

        Assert.True(result.IsSuccess);
        var trade = Assert.IsType<Trade>(result.Trade);
        Assert.Equal(draft.ImportedAtUtc, trade.ImportedAtUtc);
    }

    [Fact]
    public void Update_WithValidDraft_ChangesValuesAndPreservesIdentity()
    {
        var trade = Assert.IsType<Trade>(Trade.Create(CreateValidDraft()).Trade);
        var originalId = trade.Id;
        var updatedDraft = CreateValidDraft() with
        {
            Instrument = "  AAPL  ",
            Direction = TradeDirection.Sell,
            Currency = " usd ",
            OpeningPrice = 225m,
            ClosingPrice = 215m,
            Quantity = 4m,
            RealizedProfitLoss = 40m
        };

        var result = trade.Update(updatedDraft);

        Assert.True(result.IsSuccess);
        Assert.Same(trade, result.Trade);
        Assert.Equal(originalId, trade.Id);
        Assert.Equal("AAPL", trade.Instrument);
        Assert.Equal(TradeDirection.Sell, trade.Direction);
        Assert.Equal("USD", trade.Currency);
        Assert.Equal(40m, trade.RealizedProfitLoss);
    }

    [Fact]
    public void Update_WithInvalidDraft_ReturnsErrorsWithoutChangingTrade()
    {
        var trade = Assert.IsType<Trade>(Trade.Create(CreateValidDraft()).Trade);
        var originalInstrument = trade.Instrument;
        var originalOpeningPrice = trade.OpeningPrice;
        var invalidDraft = CreateValidDraft() with
        {
            Instrument = " ",
            OpeningPrice = 0m
        };

        var result = trade.Update(invalidDraft);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Trade);
        AssertHasError(result, nameof(TradeDraft.Instrument), TradeValidationCode.Required);
        AssertHasError(result, nameof(TradeDraft.OpeningPrice), TradeValidationCode.InvalidValue);
        Assert.Equal(originalInstrument, trade.Instrument);
        Assert.Equal(originalOpeningPrice, trade.OpeningPrice);
    }

    private static TradeDraft CreateValidDraft() => new(
        Instrument: "EUR/USD",
        Direction: TradeDirection.Buy,
        OpenedAtUtc: new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero),
        ClosedAtUtc: new DateTimeOffset(2026, 8, 4, 10, 30, 0, TimeSpan.Zero),
        OpeningPrice: 1.1500m,
        ClosingPrice: 1.1510m,
        Quantity: 1_000m,
        RealizedProfitLoss: 10m,
        Currency: "USD",
        Fees: 0.50m,
        FinancingCosts: null,
        Source: TradeDataSource.Manual,
        ImportedAtUtc: null);

    private static void AssertHasError(
        TradeCreationResult result,
        string propertyName,
        TradeValidationCode code)
    {
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == propertyName && error.Code == code);
    }

    private static void AssertHasError(
        TradeUpdateResult result,
        string propertyName,
        TradeValidationCode code)
    {
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == propertyName && error.Code == code);
    }
}
