using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class PairTradingStrategyStateEntity
{
    [Key]
    public required Guid? BacktestId { get; init; }

    public required Guid? CurrentPairGroupId { get; set; }
}
