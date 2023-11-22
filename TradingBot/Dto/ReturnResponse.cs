using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class ReturnResponse
{
    [Required]
    public required double Return { get; init; }

    [Required]
    public required DateTimeOffset Time { get; init; }
}
