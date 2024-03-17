using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public sealed class BuyLosersWithPredictionsStrategy : BuyLosersStrategyBase
{
    private readonly IMarketDataSource _marketDataSource;
    private readonly IPricePredictor _predictor;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyLosersWithPredictionsStrategy(ICurrentTradingTask tradingTask,
        IBuyLosersStrategyStateService stateService,
        IMarketDataSource marketDataSource, IAssetsDataSource assetsDataSource, IPricePredictor predictor)
        : base(tradingTask, stateService, marketDataSource, assetsDataSource)
    {
        _tradingTask = tradingTask;
        _marketDataSource = marketDataSource;
        _predictor = predictor;
    }

    public static string StrategyName => "Overreaction strategy with predictions";
    public override string Name => StrategyName;

    protected override async Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(
        IReadOnlyList<TradingSymbol> unownedSymbols, Assets assets, CancellationToken token)
    {
        var actions = new List<TradingAction>();
        foreach (var symbol in unownedSymbols)
        {
            var investmentValue = assets.Cash.AvailableAmount / unownedSymbols.Count;
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
            var prediction = await _predictor.GetPredictionForSingleSymbolAsync(symbol, token);
            actions.Add(TradingAction.LimitBuy(symbol, investmentValue / lastPrice,
                prediction is null ? lastPrice : GetBuyPrice(lastPrice, prediction.Prices[0].LowPrice),
                _tradingTask.GetTaskTime()));
        }

        return actions;
    }

    private static decimal GetBuyPrice(decimal last, decimal predictedLow)
    {
        return (last + predictedLow) / 2;
    }
}
