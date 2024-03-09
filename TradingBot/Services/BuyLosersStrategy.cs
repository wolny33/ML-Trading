using TradingBot.Models;

namespace TradingBot.Services;

public sealed class BuyLosersStrategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IMarketDataSource _marketDataSource;
    private readonly IBuyLosersStrategyStateService _stateService;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyLosersStrategy(ICurrentTradingTask tradingTask, IBuyLosersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource)
    {
        _tradingTask = tradingTask;
        _stateService = stateService;
        _marketDataSource = marketDataSource;
        _assetsDataSource = assetsDataSource;
    }

    public static string StrategyName => "Overreaction strategy";
    public string Name => StrategyName;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var state = await _stateService.GetStateAsync(_tradingTask.CurrentBacktestId, token);
        if (state.SymbolsToBuy.Count > 0) return await BuyPendingSymbolsAsync(state, token);

        if (state.NextEvaluationDay is { } nextDay && nextDay < _tradingTask.GetTaskDay())
            return Array.Empty<TradingAction>();

        var losers = await DetermineLosersAsync(token);

        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);
        var unwantedPositions = assets.Positions.Values.Where(p => !losers.Contains(p.Symbol)).ToList();
        var unownedLosers = losers.Where(s => !assets.Positions.ContainsKey(s)).ToList();

        await _stateService.SetSymbolsToBuyAsync(unownedLosers, _tradingTask.CurrentBacktestId, token);
        await _stateService.SetNextExecutionDay((state.NextEvaluationDay ?? _tradingTask.GetTaskDay()).AddDays(30),
            _tradingTask.CurrentBacktestId, CancellationToken.None);

        return unwantedPositions
            .Select(p => TradingAction.MarketSell(p.Symbol, p.AvailableQuantity, _tradingTask.GetTaskTime())).ToList();
    }

    private async Task<List<TradingSymbol>> DetermineLosersAsync(CancellationToken token)
    {
        var today = _tradingTask.GetTaskDay();
        var allSymbolsData = await _marketDataSource.GetPricesForAllSymbolsAsync(today, today.AddDays(-30), token);

        var lastMonthReturns = new Dictionary<TradingSymbol, decimal>();
        foreach (var (symbol, symbolData) in allSymbolsData)
            lastMonthReturns[symbol] = symbolData[^1].Close / symbolData[0].Close - 1;

        var sortedSymbols = lastMonthReturns.Keys.OrderBy(symbol => lastMonthReturns[symbol]).ToList();
        return sortedSymbols.Take(sortedSymbols.Count / 10).ToList();
    }

    private async Task<IReadOnlyList<TradingAction>> BuyPendingSymbolsAsync(BuyLosersStrategyState state,
        CancellationToken token)
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var actions = new List<TradingAction>();
        var availableMoney = assets.Cash.AvailableAmount * 0.95m;
        foreach (var (symbol, index) in state.SymbolsToBuy.Select((s, i) => (s, i)))
        {
            var investmentValue = availableMoney / state.SymbolsToBuy.Count;
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
            actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrice, _tradingTask.GetTaskTime()));
        }

        await _stateService.ClearSymbolsToBuyAsync(_tradingTask.CurrentBacktestId, token);

        return actions;
    }
}
