namespace DemoTradeLab.Api.Contracts;

public sealed record HealthResponse(string Status, DateTimeOffset CheckedAtUtc);
