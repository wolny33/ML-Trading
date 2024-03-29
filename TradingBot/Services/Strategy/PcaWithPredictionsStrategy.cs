using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services.Strategy;

public sealed class PcaWithPredictionsStrategy : PcaStrategyBase
{
    private readonly ILogger _logger;
    private readonly IMarketDataSource _marketDataSource;
    private readonly IPricePredictor _predictor;
    private readonly ICurrentTradingTask _tradingTask;

    public PcaWithPredictionsStrategy(IPcaDecompositionService decompositionService,
        IPcaDecompositionCreator decompositionCreator, IAssetsDataSource assetsDataSource,
        IMarketDataSource marketDataSource, ICurrentTradingTask tradingTask, ILogger logger,
        IStrategyParametersService strategyParameters, IPricePredictor predictor)
        : base(decompositionService, decompositionCreator, assetsDataSource, marketDataSource, tradingTask, logger,
            strategyParameters)
    {
        _logger = logger;
        _marketDataSource = marketDataSource;
        _predictor = predictor;
        _tradingTask = tradingTask;
    }

    public static string StrategyName => "PCA strategy with predictions";
    public override string Name => StrategyName;

    protected override async Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(
        IReadOnlyList<SymbolWithNormalizedDifference> undervalued, Assets assets,
        IReadOnlyDictionary<TradingSymbol, decimal> lastPrices, decimal damping, CancellationToken token)
    {
        var predictions = new Dictionary<TradingSymbol, Prediction?>();
        foreach (var (symbol, _) in undervalued)
        {
            predictions[symbol] = await _predictor.GetPredictionForSingleSymbolAsync(symbol, token);
        }

        var nonDecreasing = undervalued.Where(pair =>
                predictions[pair.Symbol] is not { } prediction ||
                prediction.Prices[0].ClosingPrice >= lastPrices[pair.Symbol])
            .ToList();

        _logger.Debug("{Count} out of {All} undervalued symbols are non-decreasing", nonDecreasing.Count,
            undervalued.Count);
        _logger.Verbose("Non-decreasing symbols: {Symbols}", nonDecreasing.Select(pair => pair.Symbol.Value));

        var sumOfDifferences = nonDecreasing.Sum(pair => pair.NormalizedDifference);

        var actions = new List<TradingAction>();
        foreach (var (symbol, difference) in nonDecreasing)
        {
            var investmentValue = assets.Cash.AvailableAmount * (decimal)(difference / sumOfDifferences);
            var buyPrice = predictions[symbol] is { } prediction
                ? GetBuyPrice(lastPrices[symbol], prediction.Prices[0].LowPrice, damping)
                : lastPrices[symbol];

            actions.Add(TradingAction.LimitBuy(symbol, (int)(investmentValue / buyPrice), buyPrice,
                _tradingTask.GetTaskTime()));
        }

        return actions;
    }

    protected override async Task<IReadOnlyList<TradingAction>> GetSellActionsAsync(IEnumerable<Position> positions,
        decimal damping, CancellationToken token)
    {
        var actions = new List<TradingAction>();
        foreach (var position in positions)
        {
            var lastPrice = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(position.Symbol, token);
            var prediction = await _predictor.GetPredictionForSingleSymbolAsync(position.Symbol, token);
            var action = TradingAction.LimitSell(position.Symbol, (int)position.AvailableQuantity,
                prediction is null ? lastPrice : GetSellPrice(lastPrice, prediction.Prices[0].HighPrice, damping),
                _tradingTask.GetTaskTime());
            actions.Add(action);

            if (position.AvailableQuantity != (int)position.AvailableQuantity)
            {
                actions.Add(TradingAction.MarketSell(position.Symbol,
                    position.AvailableQuantity - (int)position.AvailableQuantity, _tradingTask.GetTaskTime()));
            }
        }

        return actions;
    }

    private static decimal GetBuyPrice(decimal last, decimal predictedLow, decimal damping)
    {
        return last > predictedLow ? last * damping + predictedLow * (1 - damping) : predictedLow;
    }

    private static decimal GetSellPrice(decimal last, decimal predictedHigh, decimal damping)
    {
        return last < predictedHigh ? last * damping + predictedHigh * (1 - damping) : predictedHigh;
    }
}
