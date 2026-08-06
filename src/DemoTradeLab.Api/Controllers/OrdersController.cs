using DemoTradeLab.Api.Contracts.Orders;
using DemoTradeLab.Core.Orders;
using Microsoft.AspNetCore.Mvc;

namespace DemoTradeLab.Api.Controllers;

[ApiController]
[Route("api/demo-accounts/{accountId:guid}/orders")]
public sealed class OrdersController(
    OrderService service,
    ILogger<OrdersController> logger) : ControllerBase
{
    private const string GetOrderByIdRouteName = "GetOrderById";

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var orders = await service.ListAsync(accountId, cancellationToken);

        return orders is null
            ? AccountNotFound(accountId)
            : Ok(orders.Select(order => order.ToResponse()).ToArray());
    }

    [HttpGet("{orderId:guid}", Name = GetOrderByIdRouteName)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetByIdAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await service.GetByIdAsync(accountId, orderId, cancellationToken);

        return order is null
            ? OrderNotFound(orderId)
            : Ok(order.ToResponse());
    }

    [HttpGet("{orderId:guid}/events")]
    [ProducesResponseType<IReadOnlyList<OrderEventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderEventResponse>>> ListEventsAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var events = await service.ListEventsAsync(
            accountId,
            orderId,
            cancellationToken);

        return events is null
            ? OrderNotFound(orderId)
            : Ok(events.Select(orderEvent => orderEvent.ToResponse()).ToArray());
    }

    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> CreateAsync(
        Guid accountId,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            accountId,
            request.ReservationId!.Value,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return OperationProblem(result.Errors, accountId);
        }

        var response = result.Order.ToResponse();

        if (result.IsNoOp)
        {
            return Ok(response);
        }

        logger.LogInformation(
            "Created demo order {OrderId} for account {AccountId}",
            response.Id,
            accountId);

        return CreatedAtRoute(
            GetOrderByIdRouteName,
            new { accountId, orderId = response.Id },
            response);
    }

    [HttpPost("{orderId:guid}/complete")]
    public Task<ActionResult<OrderResponse>> CompleteAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            accountId,
            orderId,
            "completed",
            service.CompleteAsync,
            cancellationToken);

    [HttpPost("{orderId:guid}/fail")]
    public Task<ActionResult<OrderResponse>> MarkFailedAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            accountId,
            orderId,
            "marked as failed",
            service.MarkFailedAsync,
            cancellationToken);

    [HttpPost("{orderId:guid}/compensate")]
    public Task<ActionResult<OrderResponse>> CompensateAsync(
        Guid accountId,
        Guid orderId,
        CancellationToken cancellationToken) =>
        TransitionAsync(
            accountId,
            orderId,
            "compensated",
            service.CompensateAsync,
            cancellationToken);

    [HttpGet("reconciliation")]
    [ProducesResponseType<ReconciliationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReconciliationResponse>> ReconcileAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var report = await service.ReconcileAsync(accountId, cancellationToken);

        return report is null
            ? AccountNotFound(accountId)
            : Ok(report.ToResponse());
    }

    private async Task<ActionResult<OrderResponse>> TransitionAsync(
        Guid accountId,
        Guid orderId,
        string operation,
        Func<Guid, Guid, CancellationToken, Task<OrderOperationResult>> transition,
        CancellationToken cancellationToken)
    {
        var result = await transition(accountId, orderId, cancellationToken);

        if (!result.IsSuccess)
        {
            return OperationProblem(result.Errors, accountId);
        }

        logger.LogInformation(
            "Demo order {OrderId} for account {AccountId} was {Operation}; no-op: {IsNoOp}",
            orderId,
            accountId,
            operation,
            result.IsNoOp);

        return Ok(result.Order.ToResponse());
    }

    private ActionResult OperationProblem(
        IReadOnlyList<OrderError> errors,
        Guid accountId)
    {
        if (errors.Any(error => error.Code == OrderErrorCode.AccountNotFound))
        {
            return AccountNotFound(accountId);
        }

        var notFound = errors.FirstOrDefault(error => error.Code is
            OrderErrorCode.OrderNotFound or OrderErrorCode.ReservationNotFound);

        if (notFound is not null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Workflow resource not found",
                detail: notFound.Message);
        }

        if (errors.Any(error => error.Code is
                OrderErrorCode.InvalidState or
                OrderErrorCode.ReservationNotActive or
                OrderErrorCode.AccountMismatch))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order operation rejected",
                detail: errors[0].Message);
        }

        var validationErrors = errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray());

        return ValidationProblem(new ValidationProblemDetails(validationErrors));
    }

    private ObjectResult AccountNotFound(Guid accountId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Demo account not found",
            detail: $"Demo account '{accountId}' does not exist.");

    private ObjectResult OrderNotFound(Guid orderId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Order not found",
            detail: $"Order '{orderId}' does not exist.");
}
