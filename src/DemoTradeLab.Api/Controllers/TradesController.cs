using DemoTradeLab.Api.Contracts.Trades;
using DemoTradeLab.Core.Trades;
using Microsoft.AspNetCore.Mvc;

namespace DemoTradeLab.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController(
    TradeService tradeService,
    ILogger<TradesController> logger) : ControllerBase
{
    private const string GetTradeByIdRouteName = "GetTradeById";

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TradeResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TradeResponse>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var trades = await tradeService.ListAsync(cancellationToken);
        var response = trades.Select(trade => trade.ToResponse()).ToArray();

        return Ok(response);
    }

    [HttpGet("{id:guid}", Name = GetTradeByIdRouteName)]
    [ProducesResponseType<TradeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradeResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var trade = await tradeService.GetByIdAsync(id, cancellationToken);

        return trade is null
            ? TradeNotFound(id)
            : Ok(trade.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<TradeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TradeResponse>> CreateAsync(
        SaveTradeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tradeService.CreateAsync(
            request.ToDraft(TradeDataSource.Manual),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return DomainValidationProblem(result.Errors);
        }

        var response = result.Trade.ToResponse();

        logger.LogInformation(
            "Created manual trade {TradeId} for instrument {Instrument}",
            response.Id,
            response.Instrument);

        return CreatedAtRoute(
            GetTradeByIdRouteName,
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<TradeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradeResponse>> UpdateAsync(
        Guid id,
        SaveTradeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await tradeService.UpdateAsync(
            id,
            request.ToDraft(TradeDataSource.Manual),
            cancellationToken);

        if (result is null)
        {
            return TradeNotFound(id);
        }

        if (!result.IsSuccess)
        {
            return DomainValidationProblem(result.Errors);
        }

        var response = result.Trade.ToResponse();

        logger.LogInformation("Updated trade {TradeId}", id);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await tradeService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return TradeNotFound(id);
        }

        logger.LogInformation("Deleted trade {TradeId}", id);

        return NoContent();
    }

    private ObjectResult TradeNotFound(Guid id) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Trade not found",
            detail: $"Trade '{id}' does not exist.");

    private ActionResult DomainValidationProblem(
        IEnumerable<TradeValidationError> errors)
    {
        var validationErrors = errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray());

        return ValidationProblem(new ValidationProblemDetails(validationErrors));
    }
}
