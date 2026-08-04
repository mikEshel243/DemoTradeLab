using DemoTradeLab.Api.Contracts.Analytics;
using DemoTradeLab.Core.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace DemoTradeLab.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(TradeAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var analytics = await analyticsService.GetDashboardAsync(cancellationToken);
        return Ok(analytics.ToResponse());
    }

    [HttpGet("instruments")]
    [ProducesResponseType<IReadOnlyList<InstrumentSummaryResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstrumentSummaryResponse>>>
        GetInstrumentSummariesAsync(CancellationToken cancellationToken)
    {
        var summaries = await analyticsService.GetInstrumentSummariesAsync(
            cancellationToken);
        var response = summaries.Select(summary => summary.ToResponse()).ToArray();

        return Ok(response);
    }

    [HttpGet("profit-loss-timeline")]
    [ProducesResponseType<IReadOnlyList<CurrencyProfitLossTimelineResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CurrencyProfitLossTimelineResponse>>>
        GetProfitLossTimelineAsync(CancellationToken cancellationToken)
    {
        var timelines = await analyticsService.GetProfitLossTimelineAsync(
            cancellationToken);
        var response = timelines.Select(timeline => timeline.ToResponse()).ToArray();

        return Ok(response);
    }
}
