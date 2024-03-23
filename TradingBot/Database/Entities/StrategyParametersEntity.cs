using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public class StrategyParametersEntity
{
    [Key]
    public required Guid Id { get; set; }

    // General
    public required double LimitPriceDamping { get; set; }

    // Basic
    public required int MaxStocksBuyCount { get; set; }
    public required int MinDaysDecreasing { get; set; }
    public required int MinDaysIncreasing { get; set; }
    public required double TopGrowingSymbolsBuyRatio { get; set; }

    // Buy losers
    public required int BuyLosersEvaluationFrequencyInDays { get; set; }
    public required int BuyLosersAnalysisLengthInDays { get; set; }

    // Buy winners
    public required int BuyWinnersEvaluationFrequencyInDays { get; set; }
    public required int BuyWinnersAnalysisLengthInDays { get; set; }
    public required int BuyWinnersSimultaneousEvaluations { get; set; }
    public required int BuyWinnersBuyWaitTimeInDays { get; set; }

    // Pca
    public required double PcaVarianceFraction { get; set; }
    public required int PcaAnalysisLengthInDays { get; set; }
    public required int PcaDecompositionExpirationInDays { get; set; }
    public required double PcaUndervaluedThreshold { get; set; }
}
