using System.ComponentModel.DataAnnotations;
using DemoTradeLab.Core.Trades;

namespace DemoTradeLab.Api.Contracts.Trades;

public sealed record SaveTradeRequest
{
    [Required]
    [StringLength(100)]
    public string? Instrument { get; init; }

    [Required]
    [EnumDataType(typeof(TradeDirection))]
    public TradeDirection? Direction { get; init; }

    [Required]
    public DateTimeOffset? OpenedAtUtc { get; init; }

    [Required]
    public DateTimeOffset? ClosedAtUtc { get; init; }

    [Required]
    public decimal? OpeningPrice { get; init; }

    [Required]
    public decimal? ClosingPrice { get; init; }

    [Required]
    public decimal? Quantity { get; init; }

    [Required]
    public decimal? RealizedProfitLoss { get; init; }

    [Required]
    [RegularExpression("^[A-Za-z]{3}$")]
    public string? Currency { get; init; }

    public decimal? Fees { get; init; }

    public decimal? FinancingCosts { get; init; }
}
