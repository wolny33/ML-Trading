using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

// TODO: Validation
public sealed class StrategyParametersRequest
{
    [Required]
    public required int MaxStocksBuyCount { get; init; }

    [Required]
    public required int MinDaysDecreasing { get; init; }

    [Required]
    public required int MinDaysIncreasing { get; init; }

    [Required]
    public required double TopGrowingSymbolsBuyRatio { get; init; }
}
