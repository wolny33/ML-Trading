using Alpaca.Markets;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public sealed class BuyWinnersStrategy : IStrategy
{
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
        if (state.Evaluations.FirstOrDefault(e =>
                !e.Bought &&
                e.CreatedAt.AddDays(7) <= _tradingTask.GetTaskDay())
            is { } pending)
        {
            var buyActions = await BuyPendingSymbolsAsync(pending.SymbolsToBuy, token);
            await _stateService.MarkEvaluationAsBoughtAsync(buyActions.Select(action => action.Id).ToList(), pending.Id,
                token);
            return buyActions;
        }

        if (state.NextEvaluationDay is { } nextDay && nextDay < _tradingTask.GetTaskDay())
            return Array.Empty<TradingAction>();

        var winners = await DetermineWinnersAsync(token);
        var newEvaluation = new BuyWinnersEvaluation
        {
            Id = Guid.NewGuid(),
            CreatedAt = _tradingTask.GetTaskDay(),
            Bought = false,
            SymbolsToBuy = winners
        };

        await _stateService.SaveNewEvaluationAsync(newEvaluation, _tradingTask.CurrentBacktestId, token);
        await _stateService.SetNextExecutionDay((state.NextEvaluationDay ?? _tradingTask.GetTaskDay()).AddDays(30),
            _tradingTask.CurrentBacktestId, CancellationToken.None);

        var endingEvaluation =
            state.Evaluations.FirstOrDefault(e => e.CreatedAt.AddDays(80) <= _tradingTask.GetTaskDay());
        if (endingEvaluation is null) return Array.Empty<TradingAction>();

        var sellActions = new List<TradingAction>();
        foreach (var actionId in endingEvaluation.ActionIds)
        {
            var action = await _tradingActionQuery.GetTradingActionByIdAsync(actionId, token);
            if (action?.Status is not (OrderStatus.Fill or OrderStatus.Filled)) continue;

            sellActions.Add(TradingAction.MarketSell(action.Symbol, action.Quantity, _tradingTask.GetTaskTime()));
        }

        await _stateService.DeleteEvaluationAsync(endingEvaluation.Id, token);

        return sellActions;
    }

    private async Task<List<TradingSymbol>> DetermineWinnersAsync(CancellationToken token)
    {
        var today = _tradingTask.GetTaskDay();
        var allSymbolsData = await _marketDataSource.GetPricesForAllSymbolsAsync(today, today.AddDays(-12 * 30), token);

        var lastMonthReturns = new Dictionary<TradingSymbol, decimal>();
        foreach (var (symbol, symbolData) in allSymbolsData)
            lastMonthReturns[symbol] = symbolData[^1].Close / symbolData[0].Close - 1;

        var sortedSymbols = lastMonthReturns.Keys.OrderByDescending(symbol => lastMonthReturns[symbol]).ToList();
        return sortedSymbols.Take(sortedSymbols.Count / 10).ToList();
    }

    private async Task<IReadOnlyList<TradingAction>> BuyPendingSymbolsAsync(IReadOnlyList<TradingSymbol> symbols,
        CancellationToken token)
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var actions = new List<TradingAction>();
        var availableMoney = assets.Cash.AvailableAmount * 0.95m;
        foreach (var (symbol, index) in symbols.Select((s, i) => (s, i)))
        {
            var investmentValue = availableMoney / symbols.Count;
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
            actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrice, _tradingTask.GetTaskTime()));
        }

        return actions;
    }
}
