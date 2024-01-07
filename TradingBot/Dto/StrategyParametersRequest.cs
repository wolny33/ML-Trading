using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyParametersRequest
{
    [Required]
    [Range(1, double.PositiveInfinity)]
    public required int MaxStocksBuyCount { get; init; }

    [Required]
    [Range(1, 5)]
    public required int MinDaysDecreasing { get; init; }

    [Required]
    [Range(1, 5)]
    public required int MinDaysIncreasing { get; init; }

    [Required]
    [Range(0.01, 1)]
    public required double TopGrowingSymbolsBuyRatio { get; init; }
}
