using System.ComponentModel.DataAnnotations;
using TradingBot.Exceptions;

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

    public required DateTimeOffset? ExecutedAt { get; init; }

    public required Guid? AlpacaId { get; init; }

    public required decimal? AverageFillPrice { get; init; }

    public required ErrorResponse? Error { get; init; }
}
