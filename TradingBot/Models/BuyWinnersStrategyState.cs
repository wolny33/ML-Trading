using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class BuyWinnersStrategyState
{
    public static Guid NormalExecutionStateId => Guid.NewGuid();
    public required DateOnly? NextEvaluationDay { get; init; }
    public required IReadOnlyList<BuyWinnersEvaluation> Evaluations { get; init; }

    public static BuyWinnersStrategyState FromEntity(BuyWinnersStrategyStateEntity entity)
    {
        return new BuyWinnersStrategyState
        {
            NextEvaluationDay = entity.NextEvaluationDay,
            Evaluations = entity.Evaluations.OrderBy(e => e.CreatedAt).Select(BuyWinnersEvaluation.FromEntity)
                .ToList()
        };
    }
}

public sealed class BuyWinnersEvaluation
{
    public required Guid Id { get; init; }
    public required DateOnly CreatedAt { get; init; }
    public required bool Bought { get; init; }
    public required IReadOnlyList<TradingSymbol> SymbolsToBuy { get; init; }
    public IReadOnlyList<Guid> ActionIds { get; init; } = new List<Guid>();

    public static BuyWinnersEvaluation FromEntity(BuyWinnersEvaluationEntity entity)
    {
        return new BuyWinnersEvaluation
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            Bought = entity.Bought,
            SymbolsToBuy = entity.SymbolsToBuy.Select(s => new TradingSymbol(s.Symbol)).ToList(),
            ActionIds = entity.Actions.Select(a => a.ActionId).ToList()
        };
    }

    public BuyWinnersEvaluationEntity ToEntity(Guid backtestId)
    {
        return new BuyWinnersEvaluationEntity
        {
            Id = Id,
            StrategyStateBacktestId = backtestId,
            CreatedAt = CreatedAt,
            Bought = Bought,
            SymbolsToBuy = SymbolsToBuy.Select(s => new WinnerSymbolToBuyEntity
            {
                Id = Guid.NewGuid(),
                EvaluationId = Id,
                Symbol = s.Value
            }).ToList()
        };
    }
}
