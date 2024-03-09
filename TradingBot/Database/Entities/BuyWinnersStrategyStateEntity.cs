using System.ComponentModel.DataAnnotations;
using TradingBot.Models;

namespace TradingBot.Database.Entities;

public sealed class BuyWinnersStrategyStateEntity
{
    [Key]
    public required Guid? BacktestId { get; init; }

    public required DateOnly? NextEvaluationDay { get; set; }

    public IList<BuyWinnersEvaluationEntity> Evaluations { get; init; } = new List<BuyWinnersEvaluationEntity>();
}

public sealed class BuyWinnersEvaluationEntity
{
    [Key]
    public required Guid Id { get; init; }

    [Required]
    public BuyWinnersStrategyStateEntity StrategyState { get; init; } = null!;

    public required Guid? StrategyStateBacktestId { get; init; }
    public required DateOnly CreatedAt { get; init; }
    public required BuyWinnersEvaluationState State { get; set; }
    public required IList<WinnerSymbolToBuyEntity> SymbolsToBuy { get; init; } = new List<WinnerSymbolToBuyEntity>();
}
