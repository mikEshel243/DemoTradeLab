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
        var response = await _client.PostAsJsonAsync(
            ReservationsUrl(),
            new CreateReservationRequest { Amount = 120m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Reservation operation rejected", problem?.Title);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
        Assert.Empty((await _client.GetFromJsonAsync<ReservationResponse[]>(
            ReservationsUrl(),
            JsonOptions))!);
    }

    [Fact]
    public async Task Create_WithInvalidAmount_ReturnsAutomaticValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            ReservationsUrl(),
            new CreateReservationRequest { Amount = 0m });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateReservationRequest.Amount), problem!.Errors.Keys);
        await AssertBalancesAsync(total: 100m, reserved: 0m, available: 100m);
    }

    [Fact]
    public async Task Create_ForMissingAccount_ReturnsNotFound()
    {
        var missingAccountId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync(
            $"/api/demo-accounts/{missingAccountId}/reservations",
            new CreateReservationRequest { Amount = 10m });

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

    private async Task<ReservationResponse> CreateReservationAsync(decimal amount)
    {
        var response = await _client.PostAsJsonAsync(
            ReservationsUrl(),
            new CreateReservationRequest { Amount = amount });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return Assert.IsType<ReservationResponse>(
            await response.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions));
    }

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
