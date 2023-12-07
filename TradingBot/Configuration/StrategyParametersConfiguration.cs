using TradingBot.Database.Entities;
using TradingBot.Dto;

namespace TradingBot.Configuration
{
    public sealed class StrategyParametersConfiguration
    {
        public required int MaxStocksBuyCount { get; init; }
        public required int MinDaysDecreasing { get; init; }
        public required int MinDaysIncreasing { get; init; }
        public required decimal TopGrowingSymbolsBuyRatio { get; init; }

        public static StrategyParametersConfiguration FromEntity(StrategyParametersEntity entity)
        {
            return new StrategyParametersConfiguration
            {
                MaxStocksBuyCount = entity.MaxStocksBuyCount,
                MinDaysDecreasing = entity.MinDaysDecreasing,
                MinDaysIncreasing = entity.MinDaysIncreasing,
                TopGrowingSymbolsBuyRatio = (decimal)entity.TopGrowingSymbolsBuyRatio
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
        public static StrategyParametersEntity CreateDefault()
        {
            return new StrategyParametersEntity
            {
                Id = Guid.NewGuid(),
                MaxStocksBuyCount = 10,
                MinDaysDecreasing = 5,
                MinDaysIncreasing = 5,
                TopGrowingSymbolsBuyRatio = 0.4,
            };
        }
    }
}
