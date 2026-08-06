using DemoTradeLab.Api.Contracts.Reservations;
using DemoTradeLab.Core.Reservations;
using Microsoft.AspNetCore.Mvc;

namespace DemoTradeLab.Api.Controllers;

[ApiController]
[Route("api/demo-accounts/{accountId:guid}/reservations")]
public sealed class ReservationsController(
    ReservationService service,
    ILogger<ReservationsController> logger) : ControllerBase
{
    private const string GetReservationByIdRouteName = "GetReservationById";

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReservationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var reservations = await service.ListAsync(accountId, cancellationToken);

        return reservations is null
            ? AccountNotFound(accountId)
            : Ok(reservations.Select(reservation => reservation.ToResponse()).ToArray());
    }

    [HttpGet("{reservationId:guid}", Name = GetReservationByIdRouteName)]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetByIdAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        var reservation = await service.GetByIdAsync(
            accountId,
            reservationId,
            cancellationToken);

        return reservation is null
            ? ReservationNotFound(accountId, reservationId)
            : Ok(reservation.ToResponse());
    }

    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> CreateAsync(
        Guid accountId,
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            accountId,
            request.Amount!.Value,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return OperationProblem(result.Errors, accountId, null);
        }

        var response = result.Reservation.ToResponse();

        logger.LogInformation(
            "Created demo reservation {ReservationId} for account {AccountId}",
            response.Id,
            accountId);

        return CreatedAtRoute(
            GetReservationByIdRouteName,
            new { accountId, reservationId = response.Id },
            response);
    }

    [HttpPost("{reservationId:guid}/release")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ReservationResponse>> ReleaseAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        CompleteAsync(
            accountId,
            reservationId,
            "released",
            service.ReleaseAsync,
            cancellationToken);

    [HttpPost("{reservationId:guid}/consume")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ReservationResponse>> ConsumeAsync(
        Guid accountId,
        Guid reservationId,
        CancellationToken cancellationToken) =>
        CompleteAsync(
            accountId,
            reservationId,
            "consumed",
            service.ConsumeAsync,
            cancellationToken);

    private async Task<ActionResult<ReservationResponse>> CompleteAsync(
        Guid accountId,
        Guid reservationId,
        string operation,
        Func<Guid, Guid, CancellationToken, Task<ReservationOperationResult>> complete,
        CancellationToken cancellationToken)
    {
        var result = await complete(accountId, reservationId, cancellationToken);

        if (!result.IsSuccess)
        {
            return OperationProblem(result.Errors, accountId, reservationId);
        }

        logger.LogInformation(
            "Reservation {ReservationId} for account {AccountId} was {Operation}",
            reservationId,
            accountId,
            operation);

        return Ok(result.Reservation.ToResponse());
    }

    private ActionResult OperationProblem(
        IReadOnlyList<ReservationError> errors,
        Guid accountId,
        Guid? reservationId)
    {
        if (errors.Any(error => error.Code == ReservationErrorCode.AccountNotFound))
        {
            return AccountNotFound(accountId);
        }

        if (errors.Any(error => error.Code == ReservationErrorCode.ReservationNotFound))
        {
            return ReservationNotFound(accountId, reservationId!.Value);
        }

        if (errors.Any(error => error.Code is
                ReservationErrorCode.InsufficientFunds or
                ReservationErrorCode.ReservationNotActive or
                ReservationErrorCode.AccountMismatch or
                ReservationErrorCode.BalanceInvariantViolation))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Reservation operation rejected",
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

    private ObjectResult ReservationNotFound(Guid accountId, Guid reservationId) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Reservation not found",
            detail:
                $"Reservation '{reservationId}' does not exist for account '{accountId}'.");
}
