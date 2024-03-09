using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class SymbolToBuyEntity
{
    [Key]
    public required Guid Id { get; init; }

    [Required]
    public BuyLosersStrategyStateEntity StrategyState { get; init; } = null!;

    public required Guid? StrategyStateBacktestId { get; init; }
    public required string Symbol { get; init; }
}
