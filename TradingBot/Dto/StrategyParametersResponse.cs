using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyParametersResponse
{
    [Required]
    public required decimal LimitPriceDamping { get; init; }

    [Required]
    public required BasicStrategyOptionResponse Basic { get; init; }

    [Required]
    public required BuyLosersOptionsResponse BuyLosers { get; init; }

    [Required]
    public required BuyWinnersOptionsResponse BuyWinners { get; init; }

    [Required]
    public required PcaOptionsResponse Pca { get; init; }
}

public sealed class BasicStrategyOptionResponse
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
    ///     used to buy the second symbol, and all of the rest - to buy the third.
    /// </remarks>
    [Required]
    public required double TopGrowingSymbolsBuyRatio { get; init; }
}

public sealed class BuyLosersOptionsResponse
{
    [Required]
    public required int EvaluationFrequencyInDays { get; init; }

    [Required]
    public required int AnalysisLengthInDays { get; init; }
}

public sealed class BuyWinnersOptionsResponse
{
    [Required]
    public required int EvaluationFrequencyInDays { get; init; }

    [Required]
    public required int AnalysisLengthInDays { get; init; }

    [Required]
    public required int SimultaneousEvaluations { get; init; }

    [Required]
    public required int BuyWaitTimeInDays { get; init; }
}

public sealed class PcaOptionsResponse
{
    [Required]
    public required double VarianceFraction { get; init; }

    [Required]
    public required int AnalysisLengthInDays { get; init; }

    [Required]
    public required int DecompositionExpirationInDays { get; init; }

    [Required]
    public required double UndervaluedThreshold { get; init; }
}
