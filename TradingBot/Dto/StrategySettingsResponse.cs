using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategySettingsResponse
{
    [Required]
    public required string ImportantProperty { get; init; }
}
