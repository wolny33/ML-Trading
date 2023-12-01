using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class TradingActionResponse
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    public required DateTimeOffset CreatedAt { get; init; }

    [Required]
    public required decimal? Price { get; init; }

    [Required]
    public required decimal Quantity { get; init; }

    [Required]
    public required string Symbol { get; init; }

    [Required]
    public required string InForce { get; init; }

    [Required]
    public required string OrderType { get; init; }

    [Required]
    public required string Status { get; set; }

    public DateTimeOffset? ExecutedAt { get; init; }

    public Guid? AlpacaId { get; init; }

    public double? AverageFillPrice { get; init; }
}
