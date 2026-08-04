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

    [Fact]
    public async Task List_OnEmptyDatabase_ReturnsEmptyArray()
    {
        var trades = await _client.GetFromJsonAsync<TradeResponse[]>(
            "/api/trades",
            JsonOptions);

        Assert.NotNull(trades);
        Assert.Empty(trades);
    }

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
