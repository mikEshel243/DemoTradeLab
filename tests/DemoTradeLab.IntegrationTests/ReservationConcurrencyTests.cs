using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DemoTradeLab.Api.Contracts.DemoProfiles;
using DemoTradeLab.Api.Contracts.Reservations;
using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Core.Reservations;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DemoTradeLab.IntegrationTests;

public sealed class ReservationConcurrencyTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly ControlledAccountLockManager _lockManager;
    private readonly TradeApiFactory _factory;
    private HttpClient _client = null!;
    private Guid _accountId;

    public ReservationConcurrencyTests()
    {
        _lockManager = new ControlledAccountLockManager();
        _factory = new TradeApiFactory(services =>
        {
            services.RemoveAll<IAccountLockManager>();
            services.AddSingleton<IAccountLockManager>(_lockManager);
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        var profile = Assert.IsType<DemoProfile>(DemoProfile.Create(
            new DemoProfileDraft("concurrency-test", "Concurrency Test")).Profile);
        var account = Assert.IsType<DemoAccount>(profile.AddAccount(new DemoAccountDraft(
            "shared-account",
            "Shared Account",
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
    /// Overlaps two reservations of 80 against 100 and verifies exactly one success, one rejection, and a final available 20.
    /// </summary>
    [Fact]
    public async Task ConcurrentReservations_OfEightyAgainstOneHundred_ProduceOneSuccess()
    {
        var firstRequest = PostReservationAsync(80m, "concurrent-request-a");
        await _lockManager.FirstRequestHasLock.WaitAsync(TimeSpan.FromSeconds(2));

        var secondRequest = PostReservationAsync(80m, "concurrent-request-b");
        await _lockManager.SecondRequestAttemptedLock.WaitAsync(TimeSpan.FromSeconds(2));
        _lockManager.AllowFirstRequestToContinue();

        var responses = await Task.WhenAll(firstRequest, secondRequest);
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];

        Assert.Equal(
            1,
            responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(
            1,
            responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, _lockManager.MaximumConcurrentHolders);

        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);
        await AssertPersistedOutcomesAsync(
            expectedReservations: 1,
            expectedIdempotencyRecords: 2,
            expectedAuditEntries: 2,
            expectedCreatedOutcomes: 1,
            expectedRejectedOutcomes: 1);
    }

    /// <summary>
    /// Sends the same idempotency key concurrently and verifies one durable reservation and one replayed response.
    /// </summary>
    [Fact]
    public async Task ConcurrentDuplicateKey_ReplaysOnePersistedReservation()
    {
        const string idempotencyKey = "concurrent-duplicate";
        var firstRequest = PostReservationAsync(80m, idempotencyKey);
        await _lockManager.FirstRequestHasLock.WaitAsync(TimeSpan.FromSeconds(2));

        var secondRequest = PostReservationAsync(80m, idempotencyKey);
        await _lockManager.SecondRequestAttemptedLock.WaitAsync(TimeSpan.FromSeconds(2));
        _lockManager.AllowFirstRequestToContinue();

        var responses = await Task.WhenAll(firstRequest, secondRequest);
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        var firstReservation = await ReadReservationAsync(firstResponse);
        var secondReservation = await ReadReservationAsync(secondResponse);

        Assert.All(
            responses,
            response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Equal(firstReservation.Id, secondReservation.Id);
        Assert.Equal(
            1,
            responses.Count(response => response.Headers.Contains("Idempotency-Replayed")));
        Assert.Equal(1, _lockManager.MaximumConcurrentHolders);

        await AssertBalancesAsync(total: 100m, reserved: 80m, available: 20m);
        await AssertPersistedOutcomesAsync(
            expectedReservations: 1,
            expectedIdempotencyRecords: 1,
            expectedAuditEntries: 1,
            expectedCreatedOutcomes: 1,
            expectedRejectedOutcomes: 0);
    }

    private async Task<HttpResponseMessage> PostReservationAsync(
        decimal amount,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/demo-accounts/{_accountId}/reservations")
        {
            Content = JsonContent.Create(new CreateReservationRequest { Amount = amount })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        return await _client.SendAsync(request);
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
        Assert.True(account.ReservedBalance >= 0m);
        Assert.True(account.ReservedBalance <= account.TotalBalance);
        Assert.Equal(account.TotalBalance - account.ReservedBalance, account.AvailableBalance);
    }

    private async Task AssertPersistedOutcomesAsync(
        int expectedReservations,
        int expectedIdempotencyRecords,
        int expectedAuditEntries,
        int expectedCreatedOutcomes,
        int expectedRejectedOutcomes)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        var reservations = await context.DemoReservations.AsNoTracking().ToListAsync();
        var idempotencyRecords = await context.ReservationIdempotencyRecords
            .AsNoTracking()
            .ToListAsync();
        var auditEntries = await context.ReservationAuditEntries
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(expectedReservations, reservations.Count);
        Assert.Equal(expectedIdempotencyRecords, idempotencyRecords.Count);
        Assert.Equal(expectedAuditEntries, auditEntries.Count);
        Assert.Equal(
            expectedCreatedOutcomes,
            idempotencyRecords.Count(
                record => record.Outcome == ReservationIdempotencyOutcome.Created));
        Assert.Equal(
            expectedRejectedOutcomes,
            idempotencyRecords.Count(
                record => record.Outcome == ReservationIdempotencyOutcome.InsufficientFunds));
    }

    private static async Task<ReservationResponse> ReadReservationAsync(
        HttpResponseMessage response) =>
        Assert.IsType<ReservationResponse>(
            await response.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions));

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<ReservationStatus>(
            JsonNamingPolicy.CamelCase,
            allowIntegerValues: false));
        return options;
    }

    private sealed class ControlledAccountLockManager : IAccountLockManager
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly TaskCompletionSource<bool> _firstRequestHasLock =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _secondRequestAttemptedLock =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _releaseFirstRequest =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _acquisitionAttempts;
        private int _currentHolders;
        private int _maximumConcurrentHolders;

        public Task FirstRequestHasLock => _firstRequestHasLock.Task;

        public Task SecondRequestAttemptedLock => _secondRequestAttemptedLock.Task;

        public int MaximumConcurrentHolders => Volatile.Read(ref _maximumConcurrentHolders);

        public async Task<IAccountLockLease> AcquireAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _acquisitionAttempts);

            if (attempt == 2)
            {
                _secondRequestAttemptedLock.TrySetResult(true);
            }

            await _semaphore.WaitAsync(cancellationToken);
            var holders = Interlocked.Increment(ref _currentHolders);
            UpdateMaximumHolders(holders);

            if (attempt == 1)
            {
                _firstRequestHasLock.TrySetResult(true);
                await _releaseFirstRequest.Task.WaitAsync(cancellationToken);
            }

            return new ControlledLease(this);
        }

        public void AllowFirstRequestToContinue() =>
            _releaseFirstRequest.TrySetResult(true);

        private void Release()
        {
            Interlocked.Decrement(ref _currentHolders);
            _semaphore.Release();
        }

        private void UpdateMaximumHolders(int holders)
        {
            int currentMaximum;

            do
            {
                currentMaximum = Volatile.Read(ref _maximumConcurrentHolders);

                if (holders <= currentMaximum)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                       ref _maximumConcurrentHolders,
                       holders,
                       currentMaximum) != currentMaximum);
        }

        private sealed class ControlledLease(ControlledAccountLockManager owner)
            : IAccountLockLease
        {
            private ControlledAccountLockManager? _owner = owner;

            public ValueTask DisposeAsync()
            {
                Interlocked.Exchange(ref _owner, null)?.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}
