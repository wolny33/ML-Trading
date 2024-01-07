using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class AssetsStateResponse
{
    [Required]
    public required AssetsResponse Assets { get; init; }

    [Required]
    public required DateTimeOffset CreatedAt { get; init; }
}
