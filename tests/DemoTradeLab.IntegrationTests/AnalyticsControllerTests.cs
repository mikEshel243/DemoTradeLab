using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Api.Contracts.Analytics;
using DemoTradeLab.Api.Contracts.Trades;
using DemoTradeLab.Core.Trades;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DemoTradeLab.IntegrationTests;

public sealed class AnalyticsControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly TradeApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.DisposeTestResources();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Calls the dashboard endpoint against an empty SQLite database and verifies its zero-valued HTTP response contract.
    /// </summary>
    [Fact]
    public async Task Dashboard_OnEmptyDatabase_ReturnsEmptyStatistics()
    {
        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>(
            "/api/analytics/dashboard",
            JsonOptions);

        Assert.NotNull(dashboard);
        Assert.Equal(0, dashboard.TotalTrades);
        Assert.Null(dashboard.AverageTradeDurationMinutes);
        Assert.Empty(dashboard.CurrencyPerformance);
    }

    /// <summary>
    /// Persists representative trades and verifies dashboard, instrument, and timeline endpoints through the full HTTP stack.
    /// </summary>
    [Fact]
    public async Task AnalyticsEndpoints_WithTrades_ReturnExpectedAggregatesAndTimeline()
    {
        await CreateTradeAsync(CreateRequest(
            "EUR/USD",
            "USD",
            10m,
            openedMinute: 0,
            durationMinutes: 10));
        await CreateTradeAsync(CreateRequest(
            "EUR/USD",
            "USD",
            -4m,
            openedMinute: 20,
            durationMinutes: 20));
        await CreateTradeAsync(CreateRequest(
            "AAPL",
            "EUR",
            7m,
            openedMinute: 50,
            durationMinutes: 30));

        var dashboard = await _client.GetFromJsonAsync<DashboardResponse>(
            "/api/analytics/dashboard",
            JsonOptions);
        var instruments = await _client.GetFromJsonAsync<InstrumentSummaryResponse[]>(
            "/api/analytics/instruments",
            JsonOptions);
        var timelines = await _client.GetFromJsonAsync<CurrencyProfitLossTimelineResponse[]>(
            "/api/analytics/profit-loss-timeline",
            JsonOptions);

        Assert.NotNull(dashboard);
        Assert.Equal(3, dashboard.TotalTrades);
        Assert.Equal(2, dashboard.ProfitableTrades);
        Assert.Equal(1, dashboard.LosingTrades);
        Assert.Equal(66.67m, dashboard.WinRatePercentage);
        Assert.Equal("EUR/USD", dashboard.MostActiveInstrument);
        Assert.Equal(20m, dashboard.AverageTradeDurationMinutes);
        Assert.Equal(2, dashboard.CurrencyPerformance.Count);

        Assert.NotNull(instruments);
        var eurUsd = Assert.Single(
            instruments,
            summary => summary.Instrument == "EUR/USD" && summary.Currency == "USD");
        Assert.Equal(6m, eurUsd.TotalRealizedProfitLoss);

        Assert.NotNull(timelines);
        var usdTimeline = Assert.Single(
            timelines,
            timeline => timeline.Currency == "USD");
        Assert.Equal([10m, 6m], usdTimeline.Points.Select(
            point => point.CumulativeRealizedProfitLoss));
    }

    private async Task CreateTradeAsync(SaveTradeRequest request)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/trades",
            request,
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static SaveTradeRequest CreateRequest(
        string instrument,
        string currency,
        decimal realizedProfitLoss,
        int openedMinute,
        int durationMinutes)
    {
        var openedAtUtc = new DateTimeOffset(
            2026,
            8,
            4,
            10,
            0,
            0,
            TimeSpan.Zero).AddMinutes(openedMinute);

        return new SaveTradeRequest
        {
            Instrument = instrument,
            Direction = TradeDirection.Buy,
            OpenedAtUtc = openedAtUtc,
            ClosedAtUtc = openedAtUtc.AddMinutes(durationMinutes),
            OpeningPrice = 100m,
            ClosingPrice = 101m,
            Quantity = 1m,
            RealizedProfitLoss = realizedProfitLoss,
            Currency = currency
        };
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<TradeDirection>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        options.Converters.Add(new JsonStringEnumConverter<TradeDataSource>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));

        return options;
    }
}
