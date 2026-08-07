using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Api.Contracts.DemoProfiles;
using DemoTradeLab.Api.Contracts.Orders;
using DemoTradeLab.Api.Contracts.Reservations;
using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Orders;
using DemoTradeLab.Core.Reservations;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests;

public sealed class OrdersControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly TradeApiFactory _factory = new();
    private HttpClient _client = null!;
    private Guid _accountId;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        var profile = Assert.IsType<DemoProfile>(DemoProfile.Create(
            new DemoProfileDraft("order-api-test", "Order API Test")).Profile);
        var account = Assert.IsType<DemoAccount>(profile.AddAccount(new DemoAccountDraft(
            "main-account",
            "Main Account",
            100m,
            "USD")).Account);
        _accountId = account.Id;
        context.DemoProfiles.Add(profile);
        await context.SaveChangesAsync();

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
    /// Creates and completes an order through HTTP, then verifies consumed funds, durable events, and retry-safe completion.
    /// </summary>
    [Fact]
    public async Task CreateAndCompleteOrder_ConsumesFundsAndIsRetrySafe()
    {
        var reservation = await CreateReservationAsync(80m);
        var firstCreateResponse = await _client.PostAsJsonAsync(
            OrdersUrl(),
            new CreateOrderRequest { ReservationId = reservation.Id });
        var order = await ReadOrderAsync(firstCreateResponse);

        var retryCreateResponse = await _client.PostAsJsonAsync(
            OrdersUrl(),
            new CreateOrderRequest { ReservationId = reservation.Id });
        var retriedOrder = await ReadOrderAsync(retryCreateResponse);

        Assert.Equal(HttpStatusCode.Created, firstCreateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retryCreateResponse.StatusCode);
        Assert.Equal(order.Id, retriedOrder.Id);

        var completeResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/complete",
            content: null);
        var completed = await ReadOrderAsync(completeResponse);

        var retryCompleteResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/complete",
            content: null);
        var retriedCompletion = await ReadOrderAsync(retryCompleteResponse);

        Assert.Equal(OrderStatus.Completed, completed.Status);
        Assert.Equal(OrderStatus.Completed, retriedCompletion.Status);
        await AssertBalancesAsync(total: 20m, reserved: 0m, available: 20m);

        var events = await GetEventsAsync(order.Id);
        Assert.Equal(
            [OrderEventType.Created, OrderEventType.Completed],
            events.Select(orderEvent => orderEvent.EventType));
    }

    /// <summary>
    /// Fails, reconciles, and compensates an order, verifying that recovery work is visible before funds are released.
    /// </summary>
    [Fact]
    public async Task FailThenCompensate_ReleasesFundsAndClearsRecoveryWork()
    {
        var reservation = await CreateReservationAsync(80m);
        var order = await CreateOrderAsync(reservation.Id);

        var failResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/fail",
            content: null);
        var failed = await ReadOrderAsync(failResponse);

        Assert.Equal(OrderStatus.Failed, failed.Status);
        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);
        var beforeCompensation = await GetReconciliationAsync();
        Assert.True(beforeCompensation.IsBalanceConsistent);
        Assert.Equal(1, beforeCompensation.FailedOrdersPendingCompensation);

        var compensateResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/compensate",
            content: null);
        var compensated = await ReadOrderAsync(compensateResponse);

        var retryResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/compensate",
            content: null);
        var retriedCompensation = await ReadOrderAsync(retryResponse);

        Assert.Equal(OrderStatus.Compensated, compensated.Status);
        Assert.Equal(OrderStatus.Compensated, retriedCompensation.Status);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
        var afterCompensation = await GetReconciliationAsync();
        Assert.True(afterCompensation.IsBalanceConsistent);
        Assert.Equal(0, afterCompensation.FailedOrdersPendingCompensation);

        var events = await GetEventsAsync(order.Id);
        Assert.Equal(
            [
                OrderEventType.Created,
                OrderEventType.Failed,
                OrderEventType.Compensated
            ],
            events.Select(orderEvent => orderEvent.EventType));
    }

    /// <summary>
    /// Attempts to complete a failed order and verifies HTTP 409 with unchanged order, reservation, and account state.
    /// </summary>
    [Fact]
    public async Task Complete_FailedOrder_ReturnsConflictWithoutChangingFunds()
    {
        var reservation = await CreateReservationAsync(80m);
        var order = await CreateOrderAsync(reservation.Id);
        var failResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/fail",
            content: null);
        Assert.Equal(HttpStatusCode.OK, failResponse.StatusCode);

        var completeResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, completeResponse.StatusCode);
        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);
        var report = await GetReconciliationAsync();
        Assert.Equal(1, report.FailedOrdersPendingCompensation);
        Assert.True(report.IsBalanceConsistent);
    }

    /// <summary>
    /// Corrupts the stored reserved balance in the isolated database and verifies that reconciliation detects the mismatch.
    /// </summary>
    [Fact]
    public async Task Reconciliation_WhenPersistedBalanceIsCorrupted_ReportsMismatch()
    {
        await CreateReservationAsync(80m);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE DemoAccounts SET ReservedBalance = {70m} WHERE Id = {_accountId}");
        }

        var report = await GetReconciliationAsync();

        Assert.False(report.IsBalanceConsistent);
        Assert.Equal(70m, report.ReservedBalance);
        Assert.Equal(80m, report.ActiveReservationTotal);
    }

    /// <summary>
    /// Forces a SQLite write failure and verifies HTTP 500, full rollback, no partial events, and a successful later retry.
    /// </summary>
    [Fact]
    public async Task Complete_WhenDatabaseWriteFails_RollsBackAndCanBeRetried()
    {
        var reservation = await CreateReservationAsync(80m);
        var order = await CreateOrderAsync(reservation.Id);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE TRIGGER FailCompletedOrderEvent
                BEFORE INSERT ON DemoOrderEvents
                WHEN NEW.EventType = 'Completed'
                BEGIN
                    SELECT RAISE(ABORT, 'simulated order completion failure');
                END;
                """);
        }

        var failedResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.InternalServerError, failedResponse.StatusCode);
        var afterFailure = await _client.GetFromJsonAsync<OrderResponse>(
            OrderUrl(order.Id),
            JsonOptions);
        Assert.Equal(OrderStatus.Pending, afterFailure?.Status);
        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
            await context.Database.ExecuteSqlRawAsync(
                "DROP TRIGGER FailCompletedOrderEvent;");
        }

        var retryResponse = await _client.PostAsync(
            $"{OrderUrl(order.Id)}/complete",
            content: null);
        var completed = await ReadOrderAsync(retryResponse);

        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        Assert.Equal(OrderStatus.Completed, completed.Status);
        await AssertBalancesAsync(total: 20m, reserved: 0m, available: 20m);
        var events = await GetEventsAsync(order.Id);
        Assert.Equal(
            [OrderEventType.Created, OrderEventType.Completed],
            events.Select(orderEvent => orderEvent.EventType));
    }

    private async Task<ReservationResponse> CreateReservationAsync(decimal amount)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/demo-accounts/{_accountId}/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest { Amount = amount })
        };
        request.Headers.Add("Idempotency-Key", $"order-test-{Guid.NewGuid():N}");
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<ReservationResponse>(
            await response.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions));
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid reservationId)
    {
        using var response = await _client.PostAsJsonAsync(
            OrdersUrl(),
            new CreateOrderRequest { ReservationId = reservationId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadOrderAsync(response);
    }

    private async Task<OrderEventResponse[]> GetEventsAsync(Guid orderId) =>
        Assert.IsType<OrderEventResponse[]>(
            await _client.GetFromJsonAsync<OrderEventResponse[]>(
                $"{OrderUrl(orderId)}/events",
                JsonOptions));

    private async Task<ReconciliationResponse> GetReconciliationAsync() =>
        Assert.IsType<ReconciliationResponse>(
            await _client.GetFromJsonAsync<ReconciliationResponse>(
                $"{OrdersUrl()}/reconciliation"));

    private async Task AssertBalancesAsync(
        decimal total,
        decimal reserved,
        decimal available)
    {
        var profiles = await _client.GetFromJsonAsync<DemoProfileResponse[]>(
            "/api/demo-profiles");
        var account = Assert.Single(Assert.Single(profiles!).Accounts);

        Assert.Equal(total, account.TotalBalance);
        Assert.Equal(reserved, account.ReservedBalance);
        Assert.Equal(available, account.AvailableBalance);
    }

    private static async Task<OrderResponse> ReadOrderAsync(HttpResponseMessage response) =>
        Assert.IsType<OrderResponse>(
            await response.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions));

    private string OrdersUrl() => $"/api/demo-accounts/{_accountId}/orders";

    private string OrderUrl(Guid orderId) => $"{OrdersUrl()}/{orderId}";

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<ReservationStatus>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        options.Converters.Add(new JsonStringEnumConverter<OrderStatus>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        options.Converters.Add(new JsonStringEnumConverter<OrderEventType>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        return options;
    }
}
