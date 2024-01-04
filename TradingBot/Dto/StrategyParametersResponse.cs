using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyParametersResponse
{
    /// <summary>
    ///     Max number of stocks that can be bought during a single trading task
    /// </summary>
    [Required]
    public required int MaxStocksBuyCount { get; init; }

    /// <summary>
    ///     Minimal amount of forecasted days that symbol's price must be decreasing for, before it is sold
    /// </summary>
    [Required]
    public required int MinDaysDecreasing { get; init; }

    /// <summary>
    ///     Minimal amount of forecasted days that symbol's price must be increasing for, before it can be bought
    /// </summary>
    [Required]
    public required int MinDaysIncreasing { get; init; }

    /// <summary>
    ///     Percentage of available cash that should be invested in most promising symbol
    /// </summary>
    /// <remarks>
    ///     For example, if <see cref="MaxStocksBuyCount" /> is 3, and <see cref="TopGrowingSymbolsBuyRatio" /> is 0.5,
    ///     50% of available funds would be used to buy the fastest growing symbol, then 50% remaining funds would be
    ///     used to buy the second symbol, and 50% of the rest - to buy the third.
    /// </remarks>
    [Required]
    public required double TopGrowingSymbolsBuyRatio { get; init; }
}
