using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public abstract class BuyLosersStrategyBase : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IMarketDataSource _marketDataSource;
    private readonly IBuyLosersStrategyStateService _stateService;
    private readonly IStrategyParametersService _strategyParameters;
    private readonly ICurrentTradingTask _tradingTask;

    protected BuyLosersStrategyBase(ICurrentTradingTask tradingTask, IBuyLosersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource,
        IStrategyParametersService strategyParameters)
    {
        _tradingTask = tradingTask;
        _stateService = stateService;
        _marketDataSource = marketDataSource;
        _assetsDataSource = assetsDataSource;
        _strategyParameters = strategyParameters;
    }

    public abstract string Name { get; }

    public async Task<int> GetRequiredPastDaysAsync(CancellationToken token = default)
    {
        return (await _strategyParameters.GetConfigurationAsync(token)).BuyLosers.AnalysisLengthInDays;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var state = await _stateService.GetStateAsync(_tradingTask.CurrentBacktestId, token);
        var config = await _strategyParameters.GetConfigurationAsync(token);
        if (state.SymbolsToBuy.Count > 0)
            return await GetBuyActionsAndClearAlreadyOwnedAsync(state, config.LimitPriceDamping, token);

        if (state.NextEvaluationDay is { } nextDay && nextDay > _tradingTask.GetTaskDay())
            return Array.Empty<TradingAction>();

        var losers = await DetermineLosersAsync(config.BuyLosers.AnalysisLengthInDays, token);

        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);
        var unwantedPositions = assets.Positions.Values.Where(p => !losers.Contains(p.Symbol)).ToList();
        var unownedLosers = losers.Where(s => !assets.Positions.ContainsKey(s)).ToList();

        await _stateService.SetSymbolsToBuyAsync(unownedLosers, _tradingTask.CurrentBacktestId, token);
        await _stateService.SetNextExecutionDayAsync(
            (state.NextEvaluationDay ?? _tradingTask.GetTaskDay()).AddDays(config.BuyLosers.EvaluationFrequencyInDays),
            _tradingTask.CurrentBacktestId, CancellationToken.None);

        return unwantedPositions
            .Select(p => TradingAction.MarketSell(p.Symbol, p.AvailableQuantity, _tradingTask.GetTaskTime())).ToList();
    }

    public Task HandleDeselectionAsync(string newStrategyName, CancellationToken token = default)
    {
        if (newStrategyName == BuyLosersStrategy.StrategyName ||
            newStrategyName == BuyLosersWithPredictionsStrategy.StrategyName)
        {
            return Task.CompletedTask;
        }

        return _stateService.ClearNextExecutionDayAsync(token);
    }

    private async Task<List<TradingSymbol>> DetermineLosersAsync(int analysisLength, CancellationToken token)
    {
        var today = _tradingTask.GetTaskDay();
        var allSymbolsData =
            await _marketDataSource.GetPricesForAllSymbolsAsync(today.AddDays(-analysisLength), today, token);

        var lastMonthReturns = new Dictionary<TradingSymbol, decimal>();
        foreach (var (symbol, symbolData) in allSymbolsData)
            lastMonthReturns[symbol] = symbolData[^1].Close / symbolData[0].Close - 1;

        var sortedSymbols = lastMonthReturns.Keys.OrderBy(symbol => lastMonthReturns[symbol]).ToList();
        return sortedSymbols.Take(sortedSymbols.Count / 10).ToList();
    }

    private async Task<IReadOnlyList<TradingAction>> GetBuyActionsAndClearAlreadyOwnedAsync(
        BuyLosersStrategyState state, decimal damping, CancellationToken token)
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var unownedSymbols = new List<TradingSymbol>();
        foreach (var symbol in state.SymbolsToBuy)
        {
            if (assets.Positions.ContainsKey(symbol))
            {
                await _stateService.ClearSymbolToBuyAsync(symbol, _tradingTask.CurrentBacktestId, token);
                continue;
            }

            unownedSymbols.Add(symbol);
        }

        return await GetBuyActionsAsync(unownedSymbols, assets, damping, token);
    }

    protected abstract Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(
        IReadOnlyList<TradingSymbol> unownedSymbols, Assets assets, decimal damping, CancellationToken token);
}

public sealed class BuyLosersStrategy : BuyLosersStrategyBase
{
    private readonly IMarketDataSource _marketDataSource;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyLosersStrategy(ICurrentTradingTask tradingTask, IBuyLosersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource,
        IStrategyParametersService strategyParameters)
        : base(tradingTask, stateService, marketDataSource, assetsDataSource, strategyParameters)
    {
        _marketDataSource = marketDataSource;
        _tradingTask = tradingTask;
    }

    public static string StrategyName => "Overreaction strategy";
    public override string Name => StrategyName;

    protected override async Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(
        IReadOnlyList<TradingSymbol> unownedSymbols, Assets assets, decimal damping, CancellationToken token)
    {
        var actions = new List<TradingAction>();
        var availableMoney = assets.Cash.AvailableAmount * 0.95m;
        foreach (var symbol in unownedSymbols)
        {
            var investmentValue = availableMoney / unownedSymbols.Count;
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
            actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrice, _tradingTask.GetTaskTime()));
        }

        return actions;
    }
}
