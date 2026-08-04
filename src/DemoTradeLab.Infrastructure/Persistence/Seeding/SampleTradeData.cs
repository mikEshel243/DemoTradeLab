using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Infrastructure.Persistence.Seeding;

internal static class SampleTradeData
{
    public static IReadOnlyList<TradeDraft> CreateDrafts() =>
    [
        CreateDraft(
            instrument: "EUR/USD",
            direction: TradeDirection.Buy,
            openedAtUtc: Utc(day: 1, hour: 9, minute: 0),
            durationMinutes: 15,
            openingPrice: 1.1000m,
            closingPrice: 1.1045m,
            quantity: 10_000m,
            realizedProfitLoss: 45m),
        CreateDraft(
            instrument: "EUR/USD",
            direction: TradeDirection.Buy,
            openedAtUtc: Utc(day: 1, hour: 10, minute: 0),
            durationMinutes: 30,
            openingPrice: 1.1050m,
            closingPrice: 1.1030m,
            quantity: 10_000m,
            realizedProfitLoss: -20m),
        CreateDraft(
            instrument: "AAPL",
            direction: TradeDirection.Buy,
            openedAtUtc: Utc(day: 1, hour: 11, minute: 0),
            durationMinutes: 20,
            openingPrice: 210m,
            closingPrice: 218m,
            quantity: 4m,
            realizedProfitLoss: 32m),
        CreateDraft(
            instrument: "EUR/USD",
            direction: TradeDirection.Sell,
            openedAtUtc: Utc(day: 2, hour: 9, minute: 0),
            durationMinutes: 25,
            openingPrice: 1.1100m,
            closingPrice: 1.1082m,
            quantity: 10_000m,
            realizedProfitLoss: 18m),
        CreateDraft(
            instrument: "GBP/USD",
            direction: TradeDirection.Buy,
            openedAtUtc: Utc(day: 2, hour: 10, minute: 0),
            durationMinutes: 10,
            openingPrice: 1.2800m,
            closingPrice: 1.2789m,
            quantity: 10_000m,
            realizedProfitLoss: -11m),
        CreateDraft(
            instrument: "AAPL",
            direction: TradeDirection.Sell,
            openedAtUtc: Utc(day: 2, hour: 11, minute: 0),
            durationMinutes: 35,
            openingPrice: 225m,
            closingPrice: 215m,
            quantity: 4m,
            realizedProfitLoss: 40m),
        CreateDraft(
            instrument: "XAU/USD",
            direction: TradeDirection.Buy,
            openedAtUtc: Utc(day: 3, hour: 9, minute: 0),
            durationMinutes: 5,
            openingPrice: 2_400m,
            closingPrice: 2_390m,
            quantity: 1m,
            realizedProfitLoss: -10m),
        CreateDraft(
            instrument: "GBP/USD",
            direction: TradeDirection.Sell,
            openedAtUtc: Utc(day: 3, hour: 10, minute: 0),
            durationMinutes: 4,
            openingPrice: 1.2850m,
            closingPrice: 1.2820m,
            quantity: 10_000m,
            realizedProfitLoss: 30m)
    ];

    private static TradeDraft CreateDraft(
        string instrument,
        TradeDirection direction,
        DateTimeOffset openedAtUtc,
        int durationMinutes,
        decimal openingPrice,
        decimal closingPrice,
        decimal quantity,
        decimal realizedProfitLoss) =>
        new(
            Instrument: instrument,
            Direction: direction,
            OpenedAtUtc: openedAtUtc,
            ClosedAtUtc: openedAtUtc.AddMinutes(durationMinutes),
            OpeningPrice: openingPrice,
            ClosingPrice: closingPrice,
            Quantity: quantity,
            RealizedProfitLoss: realizedProfitLoss,
            Currency: "USD",
            Fees: null,
            FinancingCosts: null,
            Source: TradeDataSource.Sample,
            ImportedAtUtc: null);

    private static DateTimeOffset Utc(int day, int hour, int minute) =>
        new(2026, 7, day, hour, minute, 0, TimeSpan.Zero);
}
