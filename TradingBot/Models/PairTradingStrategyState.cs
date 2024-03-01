using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class PairTradingStrategyState
{
    public required Guid? BacktestId { get; init; }
    public required Guid? CurrentPairGroupId { get; set; }

    public static PairTradingStrategyState FromEntity(PairTradingStrategyStateEntity entity)
    {
        return new PairTradingStrategyState
        {
            BacktestId = entity.BacktestId,
            CurrentPairGroupId = entity.CurrentPairGroupId
        };
    }
}
