using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategySettingsRequest
{
    [Required]
    public required string ImportantProperty { get; init; }
}
