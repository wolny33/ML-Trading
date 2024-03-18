using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public sealed class BuyWinnersWithPredictionsStrategy : BuyWinnersStrategyBase
{
    private readonly IMarketDataSource _marketDataSource;
    private readonly IPricePredictor _predictor;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyWinnersWithPredictionsStrategy(ICurrentTradingTask tradingTask,
        IBuyWinnersStrategyStateService stateService, IMarketDataSource marketDataSource,
        IAssetsDataSource assetsDataSource, ITradingActionQuery tradingActionQuery,
        IStrategyParametersService strategyParameters, IPricePredictor predictor)
        : base(tradingTask, stateService, marketDataSource, assetsDataSource, tradingActionQuery, strategyParameters)
    {
        _marketDataSource = marketDataSource;
        _predictor = predictor;
        _tradingTask = tradingTask;
    }

    public static string StrategyName => "Trend following strategy with predictions";
    public override string Name => StrategyName;

    protected override async Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(IReadOnlyList<TradingSymbol> symbols,
        decimal usableMoney, decimal damping, CancellationToken token)
    {
        var actions = new List<TradingAction>();
        foreach (var symbol in symbols)
        {
            var investmentValue = usableMoney / symbols.Count;
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(symbol, token);
            var prediction = await _predictor.GetPredictionForSingleSymbolAsync(symbol, token);
            var buyPrice = prediction is null
                ? lastPrice
                : GetBuyPrice(lastPrice, prediction.Prices[0].LowPrice, damping);

            if (investmentValue < buyPrice) continue;

            actions.Add(TradingAction.LimitBuy(symbol, (int)(investmentValue / buyPrice), buyPrice,
                _tradingTask.GetTaskTime()));
        }

        return actions;
    }

    private static decimal GetBuyPrice(decimal last, decimal predictedLow, decimal damping)
    {
        return last > predictedLow ? last * damping + predictedLow * (1 - damping) : predictedLow;
    }
}
