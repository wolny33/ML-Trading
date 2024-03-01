using TradingBot.Models;

namespace TradingBot.Services;

public sealed class PairTradingStrategy : IStrategy
{
    private readonly IPairFinder _pairFinder;
    private readonly IPairGroupQuery _pairGroupQuery;
    private readonly ICurrentTradingTask _tradingTask;

    public PairTradingStrategy(IPairGroupQuery pairGroupQuery, IPairFinder pairFinder, ICurrentTradingTask tradingTask)
    {
        _pairGroupQuery = pairGroupQuery;
        _pairFinder = pairFinder;
        _tradingTask = tradingTask;
    }

    public static string StrategyName => "Pair trading strategy";
    public string Name => StrategyName;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var pairGroup =
            await _pairGroupQuery.GetCurrentPairGroupAsync(_tradingTask.GetTaskTime() - TimeSpan.FromDays(30), token);
        if (pairGroup is null)
        {
            if (!_pairFinder.IsPairCreationInProgress())
            {
                _pairFinder.StartNewPairGroupCreation();
            }

            return Array.Empty<TradingAction>();
        }

        throw new NotImplementedException();
    }
}
