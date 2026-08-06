using System.ComponentModel.DataAnnotations;

namespace DemoTradeLab.Api.Contracts.Reservations;

public sealed record CreateReservationRequest
{
    [Required]
    [Range(
        typeof(decimal),
        "0.00000001",
        "9999999999.99999999",
        ErrorMessage = "Amount must be between 0.00000001 and 9999999999.99999999.")]
    public decimal? Amount { get; init; }
}
