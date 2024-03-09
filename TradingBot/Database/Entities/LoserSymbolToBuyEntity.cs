using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class LoserSymbolToBuyEntity
{
    [Key]
    public required Guid Id { get; init; }

    [Required]
    public BuyLosersStrategyStateEntity StrategyState { get; init; } = null!;

    public required Guid? StrategyStateBacktestId { get; init; }
    public required string Symbol { get; init; }
}
