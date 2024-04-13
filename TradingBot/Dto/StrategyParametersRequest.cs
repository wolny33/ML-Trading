using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class StrategyParametersRequest : IValidatableObject
{
    [Required]
    [Range(0, 1)]
    public required decimal LimitPriceDamping { get; init; }

    [Required]
    public required BasicStrategyOptionRequest Basic { get; init; }

    [Required]
    public required BuyLosersOptionsRequest BuyLosers { get; init; }

    [Required]
    public required BuyWinnersOptionsRequest BuyWinners { get; init; }

    [Required]
    public required PcaOptionsRequest Pca { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var errors = new List<ValidationResult>();

        Validator.TryValidateObject(Basic, new ValidationContext(Basic), errors, true);
        Validator.TryValidateObject(BuyLosers, new ValidationContext(BuyLosers), errors, true);
        Validator.TryValidateObject(BuyWinners, new ValidationContext(BuyWinners), errors, true);
        Validator.TryValidateObject(Pca, new ValidationContext(Pca), errors, true);

        return errors;
    }
}

public sealed class BasicStrategyOptionRequest
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

public sealed class BuyLosersOptionsRequest
{
    [Required]
    [Range(1, 100)]
    public required int EvaluationFrequencyInDays { get; init; }

    [Required]
    [Range(7, 365)]
    public required int AnalysisLengthInDays { get; init; }
}

public sealed class BuyWinnersOptionsRequest
{
    [Required]
    [Range(1, 100)]
    public required int EvaluationFrequencyInDays { get; init; }

    [Required]
    [Range(7, 365)]
    public required int AnalysisLengthInDays { get; init; }

    [Required]
    [Range(1, 12)]
    public required int SimultaneousEvaluations { get; init; }

    [Required]
    [Range(0, 30)]
    public required int BuyWaitTimeInDays { get; init; }
}

public sealed class PcaOptionsRequest
{
    [Required]
    [Range(0.01, 1)]
    public required double VarianceFraction { get; init; }

    [Required]
    [Range(7, 365)]
    public required int AnalysisLengthInDays { get; init; }

    [Required]
    [Range(0, 365)]
    public required int DecompositionExpirationInDays { get; init; }

    [Required]
    [Range(0, double.PositiveInfinity)]
    public required double UndervaluedThreshold { get; init; }

    [Required]
    [Range(0, 1)]
    public required double IgnoredThreshold { get; init; }

    [Required]
    [Range(0, 1)]
    public required double DiverseThreshold { get; init; }
}
