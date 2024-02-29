using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategySelectionResponse
{
    [Required]
    public required string Name { get; init; }
}
