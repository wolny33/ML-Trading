using Alpaca.Markets;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public abstract class BuyWinnersStrategyBase : IStrategy
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

    protected BuyWinnersStrategyBase(ICurrentTradingTask tradingTask, IBuyWinnersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource, ITradingActionQuery tradingActionQuery)
    {
        _tradingTask = tradingTask;
        _stateService = stateService;
        _marketDataSource = marketDataSource;
        _assetsDataSource = assetsDataSource;
        _tradingActionQuery = tradingActionQuery;
    }

    public abstract string Name { get; }
    public int RequiredPastDays => AnalysisLength;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var state = await _stateService.GetStateAsync(_tradingTask.CurrentBacktestId, token);
        var pendingSymbolsPerEvaluation = await GetPendingSymbolsForEvaluationsAsync(state.Evaluations, token);
        if (pendingSymbolsPerEvaluation.Count > 0)
        {
            var buyActions =
                await GetBuyActionsForPendingEvaluationsAsync(pendingSymbolsPerEvaluation, state.Evaluations.Count,
                    token);
            return buyActions;
        }

        if (state.NextEvaluationDay is { } nextDay && nextDay > _tradingTask.GetTaskDay())
            return Array.Empty<TradingAction>();

        await CreateNewEvaluationAsync(state, token);

        if (state.NextEvaluationDay is null)
        {
            // If the strategy was just selected, we sell everything in preparation
            return GetSellActionsForAllHeldAssets(await _assetsDataSource.GetCurrentAssetsAsync(token));
        }

        var endingEvaluations =
            state.Evaluations.Where(e =>
                    e.CreatedAt.AddDays(EvaluationFrequency * SimultaneousEvaluations - 10) <=
                    _tradingTask.GetTaskDay())
                .ToList();

        return await GetSellActionsForEndingEvaluationsAsync(endingEvaluations, token);
    }

    public Task HandleDeselectionAsync(string newStrategyName, CancellationToken token = default)
    {
        if (newStrategyName == BuyWinnersStrategy.StrategyName ||
            newStrategyName == BuyWinnersWithPredictionsStrategy.StrategyName)
        {
            return Task.CompletedTask;
        }

        return _stateService.ClearNextExecutionDayAsync(token);
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyList<TradingSymbol>>> GetPendingSymbolsForEvaluationsAsync(
        IReadOnlyList<BuyWinnersEvaluation> evaluations, CancellationToken token)
    {
        var pendingEvaluations = evaluations
            .Where(e => !e.Bought && e.CreatedAt.AddDays(BuyWaitTime) <= _tradingTask.GetTaskDay()).ToList();

        var toBuy = new Dictionary<Guid, IReadOnlyList<TradingSymbol>>();
        foreach (var evaluation in pendingEvaluations)
        {
            var symbolsToBuy = await GetPendingSymbolsForEvaluationAsync(evaluation, token);
            if (symbolsToBuy is null || evaluation.CreatedAt.AddDays(2 * BuyWaitTime) < _tradingTask.GetTaskDay())
            {
                await _stateService.MarkEvaluationAsBoughtAsync(evaluation.Id, token);
                continue;
            }

            if (symbolsToBuy.Count > 0)
            {
                toBuy[evaluation.Id] = symbolsToBuy;
            }
        }

        return toBuy;
    }

    private async Task<IReadOnlyList<TradingSymbol>?> GetPendingSymbolsForEvaluationAsync(
        BuyWinnersEvaluation evaluation,
        CancellationToken token)
    {
        var actions = (await Task.WhenAll(evaluation.ActionIds.Select(async id =>
                await _tradingActionQuery.GetLatestTradingActionStateByIdAsync(id, token))))
            .Where(a => a is not null)
            .Select(a => a!)
            .ToList();

        var bought = actions.Where(a => a.ExecutedAt is not null)
            .Where(a => a.Status is OrderStatus.Fill or OrderStatus.Filled)
            .Select(a => a.Symbol)
            .Distinct()
            .ToList();

        var pending = actions.Where(a => a.ExecutedAt is null)
            .Select(a => a.Symbol)
            .Distinct()
            .Except(bought)
            .ToList();

        var toBuy = evaluation.SymbolsToBuy.Except(bought).Except(pending).ToList();

        if (toBuy.Count == 0 && pending.Count == 0) return null;

        return toBuy;
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

    private async Task<IReadOnlyList<TradingAction>> GetSellActionsForEndingEvaluationsAsync(
        IReadOnlyList<BuyWinnersEvaluation> endingEvaluations, CancellationToken token)
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var sellActions = new List<TradingAction>();
        foreach (var endingEvaluation in endingEvaluations)
        {
            foreach (var actionId in endingEvaluation.ActionIds)
            {
                var action = await _tradingActionQuery.GetLatestTradingActionStateByIdAsync(actionId, token);
                if (action?.Status is not (OrderStatus.Fill or OrderStatus.Filled)) continue;
                if (assets.Positions.TryGetValue(action.Symbol, out var position) &&
                    position.AvailableQuantity < action.Quantity) continue;

                sellActions.Add(TradingAction.MarketSell(action.Symbol, action.Quantity, _tradingTask.GetTaskTime()));
            }

            await _stateService.DeleteEvaluationAsync(endingEvaluation.Id, token);
        }

        return sellActions;
    }

    private IReadOnlyList<TradingAction> GetSellActionsForAllHeldAssets(Assets assets)
    {
        return assets.Positions.Values
            .Select(p => TradingAction.MarketSell(p.Symbol, p.AvailableQuantity, _tradingTask.GetTaskTime())).ToList();
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

    private async Task<IReadOnlyList<TradingAction>> GetBuyActionsForPendingEvaluationsAsync(
        IReadOnlyDictionary<Guid, IReadOnlyList<TradingSymbol>> symbolsToBuy, int activeEvaluations,
        CancellationToken token)
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var actions = new List<TradingAction>();
        var usableMoney = activeEvaluations < SimultaneousEvaluations
            ? assets.Cash.AvailableAmount / (SimultaneousEvaluations + 1 - activeEvaluations)
            : assets.Cash.AvailableAmount;

        foreach (var (evaluationId, symbols) in symbolsToBuy)
        {
            var evaluationActions = await GetBuyActionsAsync(symbols, usableMoney / symbolsToBuy.Count, token);
            await _stateService.SaveActionIdsForEvaluationAsync(evaluationActions.Select(action => action.Id).ToList(),
                evaluationId, token);
            actions.AddRange(evaluationActions);
        }

        return actions;
    }

    protected abstract Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(IReadOnlyList<TradingSymbol> symbols,
        decimal usableMoney, CancellationToken token);
}

public sealed class BuyWinnersStrategy : BuyWinnersStrategyBase
{
    private readonly IMarketDataSource _marketDataSource;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyWinnersStrategy(ICurrentTradingTask tradingTask, IBuyWinnersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource, ITradingActionQuery tradingActionQuery)
        : base(tradingTask, stateService, marketDataSource, assetsDataSource, tradingActionQuery)
    {
        _tradingTask = tradingTask;
        _marketDataSource = marketDataSource;
    }

    public static string StrategyName => "Trend following strategy";
    public override string Name => StrategyName;

    protected override async Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(IReadOnlyList<TradingSymbol> symbols,
        decimal usableMoney, CancellationToken token)
    {
        var actions = new List<TradingAction>();
        foreach (var symbol in symbols)
        {
            var investmentValue = usableMoney / symbols.Count * 0.95m;
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
            actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrice, _tradingTask.GetTaskTime()));
        }

        return actions;
    }
}
