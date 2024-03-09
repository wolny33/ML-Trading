using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class BuyWinnersStrategyState
{
    public required Guid? BacktestId { get; init; }
    public required DateOnly? NextEvaluationDay { get; init; }
    public required IReadOnlyList<BuyWinnersEvaluation> Evaluations { get; init; }

    public static BuyWinnersStrategyState FromEntity(BuyWinnersStrategyStateEntity entity)
    {
        return new BuyWinnersStrategyState
        {
            BacktestId = entity.BacktestId,
            NextEvaluationDay = entity.NextEvaluationDay,
            Evaluations = entity.Evaluations.OrderBy(e => e.CreatedAt).Select(BuyWinnersEvaluation.FromEntity).ToList()
        };
    }
}

public sealed class BuyWinnersEvaluation
{
    public required Guid Id { get; init; }
    public required DateOnly CreatedAt { get; init; }
    public required BuyWinnersEvaluationState State { get; init; }
    public required IReadOnlyList<TradingSymbol> SymbolsToBuy { get; init; }

    public static BuyWinnersEvaluation FromEntity(BuyWinnersEvaluationEntity entity)
    {
        return new BuyWinnersEvaluation
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            State = entity.State,
            SymbolsToBuy = entity.SymbolsToBuy.Select(s => new TradingSymbol(s.Symbol)).ToList()
        };
    }

    public BuyWinnersEvaluationEntity ToEntity(Guid? backtestId)
    {
        return new BuyWinnersEvaluationEntity
        {
            Id = Id,
            StrategyStateBacktestId = backtestId,
            CreatedAt = CreatedAt,
            State = State,
            SymbolsToBuy = SymbolsToBuy.Select(s => new WinnerSymbolToBuyEntity
            {
                Id = Guid.NewGuid(),
                EvaluationId = Id,
                Symbol = s.Value
            }).ToList()
        };
    }
}

public enum BuyWinnersEvaluationState
{
    Waiting,
    Bought,
    Sold
}
