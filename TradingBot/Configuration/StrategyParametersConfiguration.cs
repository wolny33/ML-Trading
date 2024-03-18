using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Configuration;

public sealed class StrategyParametersConfiguration
{
    public required int MaxStocksBuyCount { get; init; }
    public required int MinDaysDecreasing { get; init; }
    public required int MinDaysIncreasing { get; init; }
    public required double TopGrowingSymbolsBuyRatio { get; init; }

    public static StrategyParametersConfiguration FromEntity(StrategyParametersEntity entity)
    {
        return new StrategyParametersConfiguration
        {
            MaxStocksBuyCount = entity.MaxStocksBuyCount,
            MinDaysDecreasing = entity.MinDaysDecreasing,
            MinDaysIncreasing = entity.MinDaysIncreasing,
            TopGrowingSymbolsBuyRatio = entity.TopGrowingSymbolsBuyRatio
        };
    }

    public static StrategyParametersConfiguration FromRequest(StrategyParametersRequest request)
    {
        return new StrategyParametersConfiguration
        {
            MaxStocksBuyCount = request.MaxStocksBuyCount,
            MinDaysDecreasing = request.MinDaysDecreasing,
            MinDaysIncreasing = request.MinDaysIncreasing,
            TopGrowingSymbolsBuyRatio = request.TopGrowingSymbolsBuyRatio
        };
    }

    public StrategyParametersResponse ToResponse()
    {
        return new StrategyParametersResponse
        {
            MaxStocksBuyCount = MaxStocksBuyCount,
            MinDaysDecreasing = MinDaysDecreasing,
            MinDaysIncreasing = MinDaysIncreasing,
            TopGrowingSymbolsBuyRatio = TopGrowingSymbolsBuyRatio
        };
    }

    public void UpdateEntity(StrategyParametersEntity entity)
    {
        entity.MaxStocksBuyCount = MaxStocksBuyCount;
        entity.MinDaysDecreasing = MinDaysDecreasing;
        entity.MinDaysIncreasing = MinDaysIncreasing;
        entity.TopGrowingSymbolsBuyRatio = TopGrowingSymbolsBuyRatio;
    }

    public static StrategyParametersEntity CreateDefault()
    {
        return new StrategyParametersEntity
        {
            Id = Guid.NewGuid(),
            MaxStocksBuyCount = 10,
            MinDaysDecreasing = 5,
            MinDaysIncreasing = 5,
            TopGrowingSymbolsBuyRatio = 0.4
        };
    }
}
