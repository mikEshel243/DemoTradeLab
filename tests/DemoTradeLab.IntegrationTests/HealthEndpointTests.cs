using System.Net;
using System.Net.Http.Json;
using DemoTradeLab.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DemoTradeLab.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    /// <summary>
    /// Calls the health endpoint through the in-memory test server and verifies a successful typed health response.
    /// </summary>
    [Fact]
    public async Task GetHealth_ReturnsHealthyResponse()
    {
        var beforeRequest = DateTimeOffset.UtcNow;

        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(health);
        Assert.Equal("Healthy", health.Status);
        Assert.True(health.CheckedAtUtc >= beforeRequest);
        Assert.True(health.CheckedAtUtc <= DateTimeOffset.UtcNow);
    }
}
