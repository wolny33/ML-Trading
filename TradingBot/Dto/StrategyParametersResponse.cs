using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyParametersResponse
{
    [Required]
    public required int MaxStocksBuyCount { get; init; }
    public required int MinDaysDecreasing { get; init; }
    public required decimal TopGrowingSymbolsBuyRatio { get; init; }
}
