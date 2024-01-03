using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyParametersResponse
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
