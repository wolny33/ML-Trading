using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class BuyLosersStrategyStateEntity
{
    [Key]
    public required Guid? BacktestId { get; init; }

    public DateOnly? NextEvaluationDay { get; set; }
    public IList<SymbolToBuyEntity> SymbolsToBuy { get; init; } = new List<SymbolToBuyEntity>();
}
