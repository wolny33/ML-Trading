using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class BuyLosersStrategyState
{
    public required DateOnly? NextEvaluationDay { get; init; }
    public required IReadOnlyList<TradingSymbol> SymbolsToBuy { get; init; }

    public static BuyLosersStrategyState FromEntity(BuyLosersStrategyStateEntity entity)
    {
        return new BuyLosersStrategyState
        {
            NextEvaluationDay = entity.NextEvaluationDay,
            SymbolsToBuy = entity.SymbolsToBuy.Select(s => new TradingSymbol(s.Symbol)).ToList()
        };
    }
}
