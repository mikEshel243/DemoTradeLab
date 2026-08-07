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

    /// <summary>
    /// Runs create, read, list, and release through HTTP and verifies the reservation lifecycle and persisted balances.
    /// </summary>
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

        var releaseResponse = await PostCompletionAsync(
            created.Id,
            "release",
            "release-lifecycle-1");
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var released = await releaseResponse.Content.ReadFromJsonAsync<ReservationResponse>(
            JsonOptions);
        Assert.NotNull(released);
        Assert.Equal(ReservationStatus.Released, released.Status);
        Assert.NotNull(released.CompletedAtUtc);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
    }

    /// <summary>
    /// Consumes an active reservation through HTTP and verifies the corresponding total and reserved balance reductions.
    /// </summary>
    [Fact]
    public async Task Consume_ActiveReservation_ReducesPersistedTotalBalance()
    {
        var created = await CreateReservationAsync(80m);

        var consumeResponse = await PostCompletionAsync(
            created.Id,
            "consume",
            "consume-lifecycle-1");

        Assert.Equal(HttpStatusCode.OK, consumeResponse.StatusCode);
        var consumed = await consumeResponse.Content.ReadFromJsonAsync<ReservationResponse>(
            JsonOptions);
        Assert.NotNull(consumed);
        Assert.Equal(ReservationStatus.Consumed, consumed.Status);
        await AssertBalancesAsync(total: 20m, reserved: 0m, available: 20m);
    }

    /// <summary>
    /// Requests more funds than are available and verifies HTTP 409, a durable rejection, and unchanged account balances.
    /// </summary>
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

    /// <summary>
    /// Posts an invalid reservation amount and verifies automatic DTO validation returns HTTP 400 before persistence.
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidAmount_ReturnsAutomaticValidationProblem()
    {
        var response = await PostCreateRequestAsync(0m, "invalid-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateReservationRequest.Amount), problem!.Errors.Keys);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
    }

    /// <summary>
    /// Targets an unknown account and verifies that reservation creation returns HTTP 404 without stored side effects.
    /// </summary>
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

    /// <summary>
    /// Attempts to release an unknown reservation and verifies HTTP 404 without changing the owning account.
    /// </summary>
    [Fact]
    public async Task Release_MissingReservation_ReturnsNotFound()
    {
        var response = await PostCompletionAsync(
            Guid.NewGuid(),
            "release",
            "missing-release-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Reservation not found", problem?.Title);
    }

    /// <summary>
    /// Attempts to consume a released reservation and verifies HTTP 409 without applying a second balance mutation.
    /// </summary>
    [Fact]
    public async Task Consume_ReleasedReservation_ReturnsConflictWithoutSecondBalanceChange()
    {
        var created = await CreateReservationAsync(80m);
        var releaseResponse = await PostCompletionAsync(
            created.Id,
            "release",
            "release-before-consume-1");
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var consumeResponse = await PostCompletionAsync(
            created.Id,
            "consume",
            "consume-after-release-1");

        Assert.Equal(HttpStatusCode.Conflict, consumeResponse.StatusCode);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
        var persisted = await _client.GetFromJsonAsync<ReservationResponse>(
            ReservationUrl(created.Id),
            JsonOptions);
        Assert.Equal(ReservationStatus.Released, persisted?.Status);
    }

    /// <summary>
    /// Lists reservations for an unknown account and verifies that the parent-resource boundary returns HTTP 404.
    /// </summary>
    [Fact]
    public async Task List_ForMissingAccount_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            $"/api/demo-accounts/{Guid.NewGuid()}/reservations");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Repeats creation with the same idempotency key and verifies one reservation, one balance change, and replay metadata.
    /// </summary>
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

    /// <summary>
    /// Reuses a creation key with another amount and verifies HTTP 409 without another reservation or balance change.
    /// </summary>
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

    /// <summary>
    /// Omits the required creation idempotency header and verifies an HTTP 400 validation Problem Details response.
    /// </summary>
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

    /// <summary>
    /// Repeats release with the same key and verifies replay without a second audit entry or balance mutation.
    /// </summary>
    [Fact]
    public async Task Release_WithSameIdempotencyKey_ReplaysWithoutSecondAuditOrBalanceChange()
    {
        var created = await CreateReservationAsync(80m);
        const string completionKey = "release-replay-1";
        var firstResponse = await PostCompletionAsync(
            created.Id,
            "release",
            completionKey);

        var replayResponse = await PostCompletionAsync(
            created.Id,
            "release",
            completionKey);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Equal(
            "true",
            Assert.Single(replayResponse.Headers.GetValues("Idempotency-Replayed")));
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        Assert.Single(context.ReservationCompletionRecords);
        Assert.Equal(
            1,
            context.ReservationAuditEntries.Count(
                entry => entry.EventType == ReservationAuditEventType.Released));
    }

    /// <summary>
    /// Reuses a completion key for another operation and verifies HTTP 409 with the original completion preserved.
    /// </summary>
    [Fact]
    public async Task Completion_WithReusedKeyForDifferentOperation_ReturnsConflict()
    {
        var created = await CreateReservationAsync(80m);
        const string completionKey = "completion-conflict-1";
        var releaseResponse = await PostCompletionAsync(
            created.Id,
            "release",
            completionKey);
        Assert.Equal(HttpStatusCode.OK, releaseResponse.StatusCode);

        var consumeResponse = await PostCompletionAsync(
            created.Id,
            "consume",
            completionKey);

        Assert.Equal(HttpStatusCode.Conflict, consumeResponse.StatusCode);
        Assert.Equal(
            "true",
            Assert.Single(consumeResponse.Headers.GetValues("Idempotency-Replayed")));
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

    private async Task<HttpResponseMessage> PostCompletionAsync(
        Guid reservationId,
        string operation,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ReservationUrl(reservationId)}/{operation}");
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
