using TradingBot.Models;

namespace TradingBot.Services;

public sealed class PairTradingStrategy : IStrategy
{
    private readonly IPairFinder _pairFinder;
    private readonly IPairGroupQuery _pairGroupQuery;
    private readonly IPairTradingStrategyStateService _stateService;
    private readonly ICurrentTradingTask _tradingTask;

    public PairTradingStrategy(IPairGroupQuery pairGroupQuery, IPairFinder pairFinder, ICurrentTradingTask tradingTask,
        IPairTradingStrategyStateService stateService)
    {
        _pairGroupQuery = pairGroupQuery;
        _pairFinder = pairFinder;
        _tradingTask = tradingTask;
        _stateService = stateService;
    }

    public static string StrategyName => "Pair trading strategy";
    public string Name => StrategyName;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var pairGroup = await GetPairGroupAsync(token);
        if (pairGroup is null)
        {
            return Array.Empty<TradingAction>();
        }

        throw new NotImplementedException();
    }

    private async Task<PairGroup?> GetPairGroupAsync(CancellationToken token = default)
    {
        var state = await _stateService.GetStateAsync(_tradingTask.CurrentBacktestId, token);
        if (state.CurrentPairGroupId is null)
        {
            // We haven't started pair group creation yet
            var pairGroupId = await _pairFinder.StartNewPairGroupCreationAsync(
                _tradingTask.GetTaskDay().AddDays(-6 * 30), _tradingTask.GetTaskDay(), _tradingTask.CurrentBacktestId);
            await _stateService.SetCurrentPairGroupIdAsync(pairGroupId, _tradingTask.CurrentBacktestId, token);
            return null;
        }

        var pairGroup = await _pairGroupQuery.GetByIdAsync(state.CurrentPairGroupId.Value, token);
        if (pairGroup is null)
        {
            // Pair group creation was started, but is still running
            return null;
        }

        // TODO: If pair group is close to expiring, queue creation of new pair group

        return pairGroup;
    }
}
