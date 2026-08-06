using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Api.Contracts.DemoProfiles;
using DemoTradeLab.Api.Contracts.Reservations;
using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests;

public sealed class ReservationsControllerTests : IAsyncLifetime
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
            new DemoProfileDraft("reservation-demo", "Reservation Demo")).Profile);
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

    [Fact]
    public async Task CreateReadListRelease_FullLifecyclePersistsExpectedState()
    {
        var created = await CreateReservationAsync(80m);

        Assert.Equal(ReservationStatus.Active, created.Status);
        Assert.Equal(80m, created.Amount);
        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);

        var loaded = await _client.GetFromJsonAsync<ReservationResponse>(
            ReservationUrl(created.Id),
            JsonOptions);
        Assert.Equal(created, loaded);

        var reservations = await _client.GetFromJsonAsync<ReservationResponse[]>(
            ReservationsUrl(),
            JsonOptions);
        Assert.Equal(created, Assert.Single(reservations!));

        var releaseResponse = await _client.PostAsync(
            $"{ReservationUrl(created.Id)}/release",
            content: null);
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var released = await releaseResponse.Content.ReadFromJsonAsync<ReservationResponse>(
            JsonOptions);
        Assert.NotNull(released);
        Assert.Equal(ReservationStatus.Released, released.Status);
        Assert.NotNull(released.CompletedAtUtc);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
    }

    [Fact]
    public async Task Consume_ActiveReservation_ReducesPersistedTotalBalance()
    {
        var created = await CreateReservationAsync(80m);

        var consumeResponse = await _client.PostAsync(
            $"{ReservationUrl(created.Id)}/consume",
            content: null);

        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);
        var consumed = await consumeResponse.Content.ReadFromJsonAsync<ReservationResponse>(
            JsonOptions);
        Assert.NotNull(consumed);
        Assert.Equal(ReservationStatus.Consumed, consumed.Status);
        await AssertBalancesAsync(total: 20m, reserved: 0m, available: 20m);
    }

    [Fact]
    public async Task Create_WithInsufficientFunds_ReturnsConflictWithoutChangingState()
    {
        var response = await PostCreateRequestAsync(120m, "insufficient-1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Reservation operation rejected", problem?.Title);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
        Assert.Empty((await _client.GetFromJsonAsync<ReservationResponse[]>(
            ReservationsUrl(),
            JsonOptions))!);

        var replayResponse = await PostCreateRequestAsync(120m, "insufficient-1");
        Assert.Equal(HttpStatusCode.Conflict, replayResponse.StatusCode);
        Assert.Equal(
            "true",
            Assert.Single(replayResponse.Headers.GetValues("Idempotency-Replayed")));

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        Assert.Empty(context.DemoReservations);
        Assert.Equal(
            ReservationIdempotencyOutcome.InsufficientFunds,
            Assert.Single(context.ReservationIdempotencyRecords).Outcome);
        Assert.Equal(
            ReservationAuditEventType.RejectedInsufficientFunds,
            Assert.Single(context.ReservationAuditEntries).EventType);
    }

    [Fact]
    public async Task Create_WithInvalidAmount_ReturnsAutomaticValidationProblem()
    {
        var response = await PostCreateRequestAsync(0m, "invalid-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateReservationRequest.Amount), problem!.Errors.Keys);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
    }

    [Fact]
    public async Task Create_ForMissingAccount_ReturnsNotFound()
    {
        var missingAccountId = Guid.NewGuid();

        var response = await PostCreateRequestAsync(
            10m,
            "missing-account-1",
            $"/api/demo-accounts/{missingAccountId}/reservations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Demo account not found", problem?.Title);
    }

    [Fact]
    public async Task Release_MissingReservation_ReturnsNotFound()
    {
        var response = await _client.PostAsync(
            $"{ReservationUrl(Guid.NewGuid())}/release",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Reservation not found", problem?.Title);
    }

    [Fact]
    public async Task Consume_ReleasedReservation_ReturnsConflictWithoutSecondBalanceChange()
    {
        var created = await CreateReservationAsync(80m);
        var releaseResponse = await _client.PostAsync(
            $"{ReservationUrl(created.Id)}/release",
            content: null);
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var consumeResponse = await _client.PostAsync(
            $"{ReservationUrl(created.Id)}/consume",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, consumeResponse.StatusCode);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
        var persisted = await _client.GetFromJsonAsync<ReservationResponse>(
            ReservationUrl(created.Id),
            JsonOptions);
        Assert.Equal(ReservationStatus.Released, persisted?.Status);
    }

    [Fact]
    public async Task List_ForMissingAccount_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            $"/api/demo-accounts/{Guid.NewGuid()}/reservations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithSameIdempotencyKey_ReplaysPersistedReservationOnce()
    {
        const string idempotencyKey = "replay-success-1";
        var firstResponse = await PostCreateRequestAsync(80m, idempotencyKey);
        var first = await ReadReservationAsync(firstResponse);

        var replayResponse = await PostCreateRequestAsync(80m, idempotencyKey);
        var replay = await ReadReservationAsync(replayResponse);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        Assert.False(firstResponse.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(
            "true",
            Assert.Single(replayResponse.Headers.GetValues("Idempotency-Replayed")));
        Assert.Equal(first.Id, replay.Id);
        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        Assert.Single(context.DemoReservations);
        Assert.Single(context.ReservationIdempotencyRecords);
        Assert.Single(context.ReservationAuditEntries);
    }

    [Fact]
    public async Task Create_WithReusedKeyAndDifferentAmount_ReturnsConflict()
    {
        const string idempotencyKey = "replay-conflict-1";
        var firstResponse = await PostCreateRequestAsync(80m, idempotencyKey);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var conflictResponse = await PostCreateRequestAsync(70m, idempotencyKey);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal(
            "true",
            Assert.Single(conflictResponse.Headers.GetValues("Idempotency-Replayed")));
        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);
    }

    [Fact]
    public async Task Create_WithoutIdempotencyKey_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            ReservationsUrl(),
            new CreateReservationRequest { Amount = 10m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Idempotency-Key", problem!.Errors.Keys);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
    }

    private async Task<ReservationResponse> CreateReservationAsync(decimal amount)
    {
        var response = await PostCreateRequestAsync(
            amount,
            $"test-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return await ReadReservationAsync(response);
    }

    private async Task<HttpResponseMessage> PostCreateRequestAsync(
        decimal amount,
        string idempotencyKey,
        string? requestUrl = null)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            requestUrl ?? ReservationsUrl())
        {
            Content = JsonContent.Create(new CreateReservationRequest { Amount = amount })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await _client.SendAsync(request);
    }

    private static async Task<ReservationResponse> ReadReservationAsync(
        HttpResponseMessage response) =>
        Assert.IsType<ReservationResponse>(
            await response.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions));

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

    private string ReservationsUrl() =>
        $"/api/demo-accounts/{_accountId}/reservations";

    private string ReservationUrl(Guid reservationId) =>
        $"{ReservationsUrl()}/{reservationId}";

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<ReservationStatus>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        return options;
    }
}
