namespace DemoTradeLab.Api.Contracts.Analytics;

public sealed record CurrencyProfitLossTimelineResponse(
    string Currency,
    IReadOnlyList<ProfitLossPointResponse> Points);
