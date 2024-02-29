using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyNamesResponse
{
    [Required]
    public required IReadOnlyList<string> Names { get; init; }
}
