using System.Net.Http.Json;
using DemoTradeLab.Api.Contracts.DemoProfiles;
using DemoTradeLab.Core.DemoProfiles;
using DemoTradeLab.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DemoTradeLab.IntegrationTests;

public sealed class DemoProfilesControllerTests : IAsyncLifetime
{
    private readonly TradeApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DemoTradeLabDbContext>();
        var profile = Assert.IsType<DemoProfile>(DemoProfile.Create(
            new DemoProfileDraft("primary-demo", "Primary Demo Profile")).Profile);
        profile.AddAccount(new DemoAccountDraft(
            "main-account",
            "Main Demo Account",
            1_000m,
            "USD"));
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
    /// Reads seeded demo profiles through HTTP and verifies persisted accounts and calculated available balances in their DTOs.
    /// </summary>
    [Fact]
    public async Task List_ReturnsPersistedProfilesAndCalculatedAvailableBalance()
    {
        var profiles = await _client.GetFromJsonAsync<DemoProfileResponse[]>(
            "/api/demo-profiles");

        var profile = Assert.Single(Assert.IsType<DemoProfileResponse[]>(profiles));
        var account = Assert.Single(profile.Accounts);
        Assert.Equal("primary-demo", profile.Key);
        Assert.Equal(1_000m, account.TotalBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal(1_000m, account.AvailableBalance);
    }
}
