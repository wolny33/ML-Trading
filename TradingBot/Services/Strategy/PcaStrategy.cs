using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public sealed class PcaStrategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IPcaDecompositionCreator _decompositionCreator;
    private readonly IPcaDecompositionService _decompositionService;
    private readonly IMarketDataSource _marketDataSource;
    private readonly ICurrentTradingTask _tradingTask;

    public PcaStrategy(IPcaDecompositionService decompositionService, IPcaDecompositionCreator decompositionCreator,
        IAssetsDataSource assetsDataSource, IMarketDataSource marketDataSource, ICurrentTradingTask tradingTask)
    {
        _decompositionService = decompositionService;
        _decompositionCreator = decompositionCreator;
        _assetsDataSource = assetsDataSource;
        _marketDataSource = marketDataSource;
        _tradingTask = tradingTask;
    }

    public static string StrategyName => "PCA strategy";
    public string Name => StrategyName;
    public int RequiredPastDays => 90;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var latestDecomposition =
            await _decompositionService.GetLatestDecompositionAsync(_tradingTask.CurrentBacktestId, token);

        // If no decomposition was created, we start the task and do nothing (since it may take a long time)
        if (latestDecomposition is null)
        {
            await _decompositionCreator.StartNewDecompositionCreationAsync(_tradingTask.CurrentBacktestId,
                _tradingTask.GetTaskDay(), token);
            return Array.Empty<TradingAction>();
        }

        var lastDayData =
            await _marketDataSource.GetPricesForAllSymbolsAsync(_tradingTask.GetTaskDay(), _tradingTask.GetTaskDay(),
                token);
        var lastPrices = lastDayData.Keys.ToDictionary(symbol => symbol, symbol => lastDayData[symbol][0].Close);
        var differences = CalculatePriceDifferences(latestDecomposition, lastPrices);

        var undervalued = differences.Where(d => d.NormalizedDifference < -1).ToList();
        var overvalued = differences.Where(d => d.NormalizedDifference > 0).ToList();

        var assets = await _assetsDataSource.GetCurrentAssetsAsync(token);

        var actions = new List<TradingAction>();

        // If haves money, buy undervalued symbols
        if (assets.Cash.AvailableAmount > assets.EquityValue * 0.01m)
        {
            var availableMoney = assets.Cash.AvailableAmount;
            foreach (var (symbol, _) in undervalued)
            {
                var investmentValue = availableMoney * 0.3m;
                actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrices[symbol],
                    _tradingTask.GetTaskTime()));
            }
        }

        // Sell overvalued held symbols
        foreach (var position in assets.Positions.Values.Where(p => overvalued.Any(s => s.Symbol == p.Symbol)))
        {
            actions.Add(TradingAction.MarketSell(position.Symbol, position.AvailableQuantity,
                _tradingTask.GetTaskTime()));
        }

        return actions;
    }

    private IReadOnlyList<SymbolWithNormalizedPriceDifference> CalculatePriceDifferences(PcaDecomposition decomposition,
        IReadOnlyDictionary<TradingSymbol, decimal> lastPrices)
    {
        throw new NotImplementedException();
    }

    private sealed record SymbolWithNormalizedPriceDifference(TradingSymbol Symbol, double NormalizedDifference);
}
