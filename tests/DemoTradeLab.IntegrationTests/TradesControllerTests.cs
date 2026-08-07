using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Api.Contracts.Trades;
using DemoTradeLab.Core.Trades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DemoTradeLab.IntegrationTests;

public sealed class TradesControllerTests : IAsyncLifetime
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
    /// Lists trades from an empty migrated database and verifies HTTP 200 with an empty JSON array.
    /// </summary>
    [Fact]
    public async Task List_OnEmptyDatabase_ReturnsEmptyArray()
    {
        var trades = await _client.GetFromJsonAsync<TradeResponse[]>(
            "/api/trades",
            JsonOptions);

        Assert.NotNull(trades);
        Assert.Empty(trades);
    }

    /// <summary>
    /// Executes create, read, update, and delete through HTTP and verifies every change in the SQLite-backed API.
    /// </summary>
    [Fact]
    public async Task CrudLifecycle_WithValidRequest_PersistsExpectedChanges()
    {
        var createRequest = CreateValidRequest();

        var createResponse = await _client.PostAsJsonAsync(
            "/api/trades",
            createRequest,
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var created = await createResponse.Content.ReadFromJsonAsync<TradeResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("EUR/USD", created.Instrument);
        Assert.Equal(TradeDataSource.Manual, created.Source);

        var getResponse = await _client.GetAsync(createResponse.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateRequest = createRequest with
        {
            Instrument = "  AAPL  ",
            Direction = TradeDirection.Sell,
            OpeningPrice = 225m,
            ClosingPrice = 215m,
            Quantity = 4m,
            RealizedProfitLoss = 40m,
            Currency = "usd"
        };

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/trades/{created.Id}",
            updateRequest,
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<TradeResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("AAPL", updated.Instrument);
        Assert.Equal(TradeDirection.Sell, updated.Direction);
        Assert.Equal("USD", updated.Currency);
        Assert.Equal(TradeDataSource.Manual, updated.Source);

        var deleteResponse = await _client.DeleteAsync($"/api/trades/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await _client.GetAsync($"/api/trades/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    /// <summary>
    /// Posts a closing time before the opening time and verifies an HTTP 400 validation Problem Details response.
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidTimeRange_ReturnsValidationProblemDetails()
    {
        var request = CreateValidRequest() with
        {
            ClosedAtUtc = CreateValidRequest().OpenedAtUtc
        };

        var response = await _client.PostAsJsonAsync(
            "/api/trades",
            request,
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(SaveTradeRequest.ClosedAtUtc), problem.Errors.Keys);
    }

    /// <summary>
    /// Omits required request fields and verifies automatic API-boundary validation before the use case executes.
    /// </summary>
    [Fact]
    public async Task Create_WithMissingRequiredFields_ReturnsValidationProblemDetails()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/trades",
            new SaveTradeRequest(),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(SaveTradeRequest.Instrument), problem.Errors.Keys);
        Assert.Contains(nameof(SaveTradeRequest.Direction), problem.Errors.Keys);
        Assert.Contains(nameof(SaveTradeRequest.OpeningPrice), problem.Errors.Keys);
    }

    /// <summary>
    /// Queries the HTTP list endpoint with combined filters and sorting and verifies only the ordered matches are returned.
    /// </summary>
    [Fact]
    public async Task List_WithFiltersAndSorting_ReturnsMatchingTradesInRequestedOrder()
    {
        await CreateTradeAsync(CreateValidRequest() with
        {
            RealizedProfitLoss = 2.10m
        });
        await CreateTradeAsync(CreateValidRequest() with
        {
            Direction = TradeDirection.Sell,
            OpenedAtUtc = CreateValidRequest().OpenedAtUtc.GetValueOrDefault().AddHours(1),
            ClosedAtUtc = CreateValidRequest().ClosedAtUtc.GetValueOrDefault().AddHours(1),
            RealizedProfitLoss = 10.05m
        });
        await CreateTradeAsync(CreateValidRequest() with
        {
            OpenedAtUtc = CreateValidRequest().OpenedAtUtc.GetValueOrDefault().AddHours(2),
            ClosedAtUtc = CreateValidRequest().ClosedAtUtc.GetValueOrDefault().AddHours(2),
            RealizedProfitLoss = -5m
        });

        var trades = await _client.GetFromJsonAsync<TradeResponse[]>(
            "/api/trades?instrument=eur%2Fusd&currency=usd&outcome=profitable" +
            "&sortBy=realizedProfitLoss&sortDirection=descending",
            JsonOptions);

        Assert.NotNull(trades);
        Assert.Equal([10.05m, 2.10m], trades.Select(trade => trade.RealizedProfitLoss));
    }

    /// <summary>
    /// Sends a non-UTC date filter and verifies that the API rejects ambiguous time-zone input with HTTP 400.
    /// </summary>
    [Fact]
    public async Task List_WithNonUtcDateFilter_ReturnsValidationProblemDetails()
    {
        var timestamp = Uri.EscapeDataString("2026-08-04T10:00:00+02:00");

        var response = await _client.GetAsync(
            $"/api/trades?closedFromUtc={timestamp}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(ListTradesQueryRequest.ClosedFromUtc), problem.Errors.Keys);
    }

    private static SaveTradeRequest CreateValidRequest() => new()
    {
        Instrument = "EUR/USD",
        Direction = TradeDirection.Buy,
        OpenedAtUtc = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero),
        ClosedAtUtc = new DateTimeOffset(2026, 8, 4, 10, 20, 0, TimeSpan.Zero),
        OpeningPrice = 1.1500m,
        ClosingPrice = 1.1520m,
        Quantity = 1_000m,
        RealizedProfitLoss = 2m,
        Currency = "USD",
        Fees = null,
        FinancingCosts = null
    };

    private async Task CreateTradeAsync(SaveTradeRequest request)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/trades",
            request,
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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
