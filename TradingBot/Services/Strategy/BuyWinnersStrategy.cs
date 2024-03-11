using Alpaca.Markets;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public sealed class BuyWinnersStrategy : IStrategy
{
    private const int BuyWaitTime = 7;
    private const int EvaluationFrequency = 30;
    private const int AnalysisLength = 12 * 30;
    private const int SimultaneousEvaluations = 3;

    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IMarketDataSource _marketDataSource;
    private readonly IBuyWinnersStrategyStateService _stateService;
    private readonly ITradingActionQuery _tradingActionQuery;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyWinnersStrategy(ICurrentTradingTask tradingTask, IBuyWinnersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource, ITradingActionQuery tradingActionQuery)
    {
        _tradingTask = tradingTask;
        _stateService = stateService;
        _marketDataSource = marketDataSource;
        _assetsDataSource = assetsDataSource;
        _tradingActionQuery = tradingActionQuery;
    }

    public static string StrategyName => "Trend following strategy";
    public string Name => StrategyName;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var state = await _stateService.GetStateAsync(_tradingTask.CurrentBacktestId, token);
        var pendingEvaluations = state.Evaluations.Where(e =>
            !e.Bought &&
            e.CreatedAt.AddDays(BuyWaitTime) <= _tradingTask.GetTaskDay()).ToList();
        if (pendingEvaluations.Count > 0)
        {
            var buyActions =
                await GetBuyActionsForPendingEvaluations(pendingEvaluations, state.Evaluations.Count, token);
            return buyActions;
        }

        if (state.NextEvaluationDay is { } nextDay && nextDay > _tradingTask.GetTaskDay())
            return Array.Empty<TradingAction>();

        await CreateNewEvaluationAsync(state, token);

        var endingEvaluations =
            state.Evaluations.Where(e =>
                    e.CreatedAt.AddDays(EvaluationFrequency * SimultaneousEvaluations - 10) <=
                    _tradingTask.GetTaskDay())
                .ToList();

        return await GetSellActionsForEndingEvaluations(endingEvaluations, token);
    }

    private async Task CreateNewEvaluationAsync(BuyWinnersStrategyState state, CancellationToken token)
    {
        var winners = await DetermineWinnersAsync(token);
        var newEvaluation = new BuyWinnersEvaluation
        {
            Id = Guid.NewGuid(),
            CreatedAt = _tradingTask.GetTaskDay(),
            Bought = false,
            SymbolsToBuy = winners
        };

        await _stateService.SaveNewEvaluationAsync(newEvaluation, _tradingTask.CurrentBacktestId, token);
        await _stateService.SetNextExecutionDayAsync((state.NextEvaluationDay ?? _tradingTask.GetTaskDay()).AddDays(30),
            _tradingTask.CurrentBacktestId, CancellationToken.None);
    }

    private async Task<IReadOnlyList<TradingAction>> GetSellActionsForEndingEvaluations(
        IReadOnlyList<BuyWinnersEvaluation> endingEvaluations, CancellationToken token)
    {
        var sellActions = new List<TradingAction>();
        foreach (var endingEvaluation in endingEvaluations)
        {
            foreach (var actionId in endingEvaluation.ActionIds)
            {
                var action = await _tradingActionQuery.GetTradingActionByIdAsync(actionId, token);
                if (action?.Status is not (OrderStatus.Fill or OrderStatus.Filled)) continue;

                sellActions.Add(TradingAction.MarketSell(action.Symbol, action.Quantity, _tradingTask.GetTaskTime()));
            }

            await _stateService.DeleteEvaluationAsync(endingEvaluation.Id, token);
        }

        return sellActions;
    }

    private async Task<List<TradingSymbol>> DetermineWinnersAsync(CancellationToken token)
    {
        var today = _tradingTask.GetTaskDay();
        var allSymbolsData =
            await _marketDataSource.GetPricesForAllSymbolsAsync(today.AddDays(-AnalysisLength), today, token);

        var lastMonthReturns = new Dictionary<TradingSymbol, decimal>();
        foreach (var (symbol, symbolData) in allSymbolsData)
            lastMonthReturns[symbol] = symbolData[^1].Close / symbolData[0].Close - 1;

        var sortedSymbols = lastMonthReturns.Keys.OrderByDescending(symbol => lastMonthReturns[symbol]).ToList();
        return sortedSymbols.Take(sortedSymbols.Count / 10).ToList();
    }

    private async Task<IReadOnlyList<TradingAction>> GetBuyActionsForPendingEvaluations(
        IReadOnlyList<BuyWinnersEvaluation> evaluations,
        int activeEvaluations, CancellationToken token)
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var actions = new List<TradingAction>();
        var availableMoney = assets.Cash.AvailableAmount * 0.95m;
        var usableMoney = activeEvaluations < SimultaneousEvaluations
            ? availableMoney / (SimultaneousEvaluations + 1 - activeEvaluations)
            : availableMoney;

        foreach (var evaluation in evaluations)
        {
            foreach (var symbol in evaluation.SymbolsToBuy)
            {
                var investmentValue = usableMoney / evaluation.SymbolsToBuy.Count / evaluations.Count;
                var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
                actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrice, _tradingTask.GetTaskTime()));
            }

            await _stateService.MarkEvaluationAsBoughtAsync(actions.Select(action => action.Id).ToList(), evaluation.Id,
                token);
        }

        return actions;
    }
}
