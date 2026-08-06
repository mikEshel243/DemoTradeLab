using System.ComponentModel.DataAnnotations;

namespace DemoTradeLab.Api.Contracts.Orders;

public sealed record CreateOrderRequest
{
    [Required]
    public Guid? ReservationId { get; init; }
}
