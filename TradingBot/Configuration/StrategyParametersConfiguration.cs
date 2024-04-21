using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Configuration;

public sealed class StrategyParametersConfiguration
{
    public required decimal LimitPriceDamping { get; init; }
    public required BasicStrategyOptions Basic { get; init; }
    public required BuyLosersOptions BuyLosers { get; init; }
    public required BuyWinnersOptions BuyWinners { get; init; }
    public required PcaOptions Pca { get; init; }

    public static StrategyParametersConfiguration FromEntity(StrategyParametersEntity entity)
    {
        return new StrategyParametersConfiguration
        {
            LimitPriceDamping = (decimal)entity.LimitPriceDamping,
            Basic = new BasicStrategyOptions
            {
                MaxStocksBuyCount = entity.MaxStocksBuyCount,
                MinDaysDecreasing = entity.MinDaysDecreasing,
                MinDaysIncreasing = entity.MinDaysIncreasing,
                TopGrowingSymbolsBuyRatio = entity.TopGrowingSymbolsBuyRatio
            },
            BuyLosers = new BuyLosersOptions
            {
                AnalysisLengthInDays = entity.BuyLosersAnalysisLengthInDays,
                EvaluationFrequencyInDays = entity.BuyLosersEvaluationFrequencyInDays
            },
            BuyWinners = new BuyWinnersOptions
            {
                AnalysisLengthInDays = entity.BuyWinnersAnalysisLengthInDays,
                EvaluationFrequencyInDays = entity.BuyWinnersEvaluationFrequencyInDays,
                BuyWaitTimeInDays = entity.BuyWinnersBuyWaitTimeInDays,
                SimultaneousEvaluations = entity.BuyWinnersSimultaneousEvaluations
            },
            Pca = new PcaOptions
            {
                AnalysisLengthInDays = entity.PcaAnalysisLengthInDays,
                DecompositionExpirationInDays = entity.PcaDecompositionExpirationInDays,
                UndervaluedThreshold = entity.PcaUndervaluedThreshold,
                VarianceFraction = entity.PcaVarianceFraction,
                DiverseThreshold = entity.PcaDiverseThreshold,
                IgnoredThreshold = entity.PcaIgnoredThreshold
            }
        };
    }

    public static StrategyParametersConfiguration FromRequest(StrategyParametersRequest request)
    {
        return new StrategyParametersConfiguration
        {
            LimitPriceDamping = request.LimitPriceDamping,
            Basic = BasicStrategyOptions.FromRequest(request.Basic),
            BuyLosers = BuyLosersOptions.FromRequest(request.BuyLosers),
            BuyWinners = BuyWinnersOptions.FromRequest(request.BuyWinners),
            Pca = PcaOptions.FromRequest(request.Pca)
        };
    }

    public StrategyParametersResponse ToResponse()
    {
        return new StrategyParametersResponse
        {
            LimitPriceDamping = LimitPriceDamping,
            Basic = Basic.ToResponse(),
            BuyLosers = BuyLosers.ToResponse(),
            BuyWinners = BuyWinners.ToResponse(),
            Pca = Pca.ToResponse()
        };
    }

    public void UpdateEntity(StrategyParametersEntity entity)
    {
        entity.LimitPriceDamping = (double)LimitPriceDamping;

        entity.MaxStocksBuyCount = Basic.MaxStocksBuyCount;
        entity.MinDaysDecreasing = Basic.MinDaysDecreasing;
        entity.MinDaysIncreasing = Basic.MinDaysIncreasing;
        entity.TopGrowingSymbolsBuyRatio = Basic.TopGrowingSymbolsBuyRatio;

        entity.BuyLosersAnalysisLengthInDays = BuyLosers.AnalysisLengthInDays;
        entity.BuyLosersEvaluationFrequencyInDays = BuyLosers.EvaluationFrequencyInDays;

        entity.BuyWinnersSimultaneousEvaluations = BuyWinners.SimultaneousEvaluations;
        entity.BuyWinnersAnalysisLengthInDays = BuyWinners.AnalysisLengthInDays;
        entity.BuyWinnersEvaluationFrequencyInDays = BuyWinners.EvaluationFrequencyInDays;
        entity.BuyWinnersBuyWaitTimeInDays = BuyWinners.BuyWaitTimeInDays;

        entity.PcaUndervaluedThreshold = Pca.UndervaluedThreshold;
        entity.PcaVarianceFraction = Pca.VarianceFraction;
        entity.PcaAnalysisLengthInDays = Pca.AnalysisLengthInDays;
        entity.PcaDecompositionExpirationInDays = Pca.DecompositionExpirationInDays;
        entity.PcaIgnoredThreshold = Pca.IgnoredThreshold;
        entity.PcaDiverseThreshold = Pca.DiverseThreshold;
    }

    public static StrategyParametersEntity CreateDefault()
    {
        return new StrategyParametersEntity
        {
            Id = Guid.NewGuid(),

            LimitPriceDamping = 0.5,

            MaxStocksBuyCount = 10,
            MinDaysDecreasing = 5,
            MinDaysIncreasing = 5,
            TopGrowingSymbolsBuyRatio = 0.4,

            BuyLosersAnalysisLengthInDays = 30,
            BuyLosersEvaluationFrequencyInDays = 30,

            BuyWinnersSimultaneousEvaluations = 3,
            BuyWinnersAnalysisLengthInDays = 12 * 30,
            BuyWinnersEvaluationFrequencyInDays = 30,
            BuyWinnersBuyWaitTimeInDays = 7,

            PcaVarianceFraction = 0.9,
            PcaUndervaluedThreshold = 1,
            PcaAnalysisLengthInDays = 3 * 30,
            PcaDecompositionExpirationInDays = 7,
            PcaDiverseThreshold = 0.5,
            PcaIgnoredThreshold = 0.25
        };
    }
}

public sealed class BasicStrategyOptions
{
    public required int MaxStocksBuyCount { get; init; }
    public required int MinDaysDecreasing { get; init; }
    public required int MinDaysIncreasing { get; init; }
    public required double TopGrowingSymbolsBuyRatio { get; init; }

    public static BasicStrategyOptions FromRequest(BasicStrategyOptionRequest request)
    {
        return new BasicStrategyOptions
        {
            MaxStocksBuyCount = request.MaxStocksBuyCount,
            MinDaysDecreasing = request.MinDaysDecreasing,
            MinDaysIncreasing = request.MinDaysIncreasing,
            TopGrowingSymbolsBuyRatio = request.TopGrowingSymbolsBuyRatio
        };
    }

    public BasicStrategyOptionResponse ToResponse()
    {
        return new BasicStrategyOptionResponse
        {
            MinDaysDecreasing = MinDaysDecreasing,
            MinDaysIncreasing = MinDaysIncreasing,
            MaxStocksBuyCount = MaxStocksBuyCount,
            TopGrowingSymbolsBuyRatio = TopGrowingSymbolsBuyRatio
        };
    }
}

public sealed class BuyLosersOptions
{
    public required int EvaluationFrequencyInDays { get; init; }
    public required int AnalysisLengthInDays { get; init; }

    public static BuyLosersOptions FromRequest(BuyLosersOptionsRequest request)
    {
        return new BuyLosersOptions
        {
            EvaluationFrequencyInDays = request.EvaluationFrequencyInDays,
            AnalysisLengthInDays = request.AnalysisLengthInDays
        };
    }

    public BuyLosersOptionsResponse ToResponse()
    {
        return new BuyLosersOptionsResponse
        {
            EvaluationFrequencyInDays = EvaluationFrequencyInDays,
            AnalysisLengthInDays = AnalysisLengthInDays
        };
    }
}

public sealed class BuyWinnersOptions
{
    public required int EvaluationFrequencyInDays { get; init; }
    public required int AnalysisLengthInDays { get; init; }
    public required int SimultaneousEvaluations { get; init; }
    public required int BuyWaitTimeInDays { get; init; }

    public static BuyWinnersOptions FromRequest(BuyWinnersOptionsRequest request)
    {
        return new BuyWinnersOptions
        {
            EvaluationFrequencyInDays = request.EvaluationFrequencyInDays,
            AnalysisLengthInDays = request.AnalysisLengthInDays,
            SimultaneousEvaluations = request.SimultaneousEvaluations,
            BuyWaitTimeInDays = request.BuyWaitTimeInDays
        };
    }

    public BuyWinnersOptionsResponse ToResponse()
    {
        return new BuyWinnersOptionsResponse
        {
            EvaluationFrequencyInDays = EvaluationFrequencyInDays,
            AnalysisLengthInDays = AnalysisLengthInDays,
            BuyWaitTimeInDays = BuyWaitTimeInDays,
            SimultaneousEvaluations = SimultaneousEvaluations
        };
    }
}

public sealed class PcaOptions
{
    public required double VarianceFraction { get; init; }
    public required int AnalysisLengthInDays { get; init; }
    public required int DecompositionExpirationInDays { get; init; }
    public required double UndervaluedThreshold { get; init; }
    public required double IgnoredThreshold { get; init; }
    public required double DiverseThreshold { get; init; }

    public static PcaOptions FromRequest(PcaOptionsRequest request)
    {
        return new PcaOptions
        {
            VarianceFraction = request.VarianceFraction,
            AnalysisLengthInDays = request.AnalysisLengthInDays,
            DecompositionExpirationInDays = request.DecompositionExpirationInDays,
            UndervaluedThreshold = request.UndervaluedThreshold,
            IgnoredThreshold = request.IgnoredThreshold,
            DiverseThreshold = request.DiverseThreshold
        };
    }

    public PcaOptionsResponse ToResponse()
    {
        return new PcaOptionsResponse
        {
            VarianceFraction = VarianceFraction,
            AnalysisLengthInDays = AnalysisLengthInDays,
            DecompositionExpirationInDays = DecompositionExpirationInDays,
            UndervaluedThreshold = UndervaluedThreshold,
            IgnoredThreshold = IgnoredThreshold,
            DiverseThreshold = DiverseThreshold
        };
    }
}
