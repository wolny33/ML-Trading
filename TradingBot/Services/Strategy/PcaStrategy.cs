using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services.Strategy;

public sealed class PcaStrategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IPcaDecompositionCreator _decompositionCreator;
    private readonly IPcaDecompositionService _decompositionService;
    private readonly ILogger _logger;
    private readonly IMarketDataSource _marketDataSource;
    private readonly ICurrentTradingTask _tradingTask;

    public PcaStrategy(IPcaDecompositionService decompositionService, IPcaDecompositionCreator decompositionCreator,
        IAssetsDataSource assetsDataSource, IMarketDataSource marketDataSource, ICurrentTradingTask tradingTask,
        ILogger logger)
    {
        _decompositionService = decompositionService;
        _decompositionCreator = decompositionCreator;
        _assetsDataSource = assetsDataSource;
        _marketDataSource = marketDataSource;
        _tradingTask = tradingTask;
        _logger = logger.ForContext<PcaStrategy>();
    }

    public static string StrategyName => "PCA strategy";
    public string Name => StrategyName;
    public int RequiredPastDays => 90;

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var latestDecomposition =
            await _decompositionService.GetLatestDecompositionAsync(_tradingTask.CurrentBacktestId, token);

        // If no decomposition was created or if decomposition is outdated, we start the task and do nothing (since it
        // may take a long time)
        if (latestDecomposition is null || latestDecomposition.ExpiresAt < _tradingTask.GetTaskDay())
        {
            if (latestDecomposition is null)
            {
                _logger.Debug("No actions were taken - no PCA decomposition exists");
            }
            else
            {
                _logger.Debug("No actions were taken - latest decomposition expired at {Expiration}",
                    latestDecomposition.ExpiresAt);
            }

            await _decompositionCreator.StartNewDecompositionCreationAsync(_tradingTask.CurrentBacktestId,
                _tradingTask.GetTaskDay(), token);
            return Array.Empty<TradingAction>();
        }

        // If decomposition expires soon, start new creation task
        if (latestDecomposition.ExpiresAt < _tradingTask.GetTaskDay().AddDays(3))
        {
            _logger.Debug("Latest decomposition will expire in less than 3 days (on {Expiration})",
                latestDecomposition.ExpiresAt);
            await _decompositionCreator.StartNewDecompositionCreationAsync(_tradingTask.CurrentBacktestId,
                _tradingTask.GetTaskDay(), token);
        }

        var lastDayData =
            await _marketDataSource.GetPricesForAllSymbolsAsync(_tradingTask.GetTaskDay(), _tradingTask.GetTaskDay(),
                token);
        var lastPrices = lastDayData.Keys.ToDictionary(symbol => symbol, symbol => lastDayData[symbol][0].Close);
        var differences = latestDecomposition.CalculatePriceDifferences(lastPrices);

        var undervalued = differences.Where(d => d.NormalizedDifference < -1).ToList();
        var overvalued = differences.Where(d => d.NormalizedDifference > 0).ToList();

        _logger.Verbose("Undervalued symbols: {Symbols}",
            undervalued.Select(pair => $"{pair.Symbol.Value} ({pair.NormalizedDifference:F2})"));
        _logger.Verbose("Overvalued symbols: {Symbols}",
            overvalued.Select(pair => $"{pair.Symbol.Value} ({pair.NormalizedDifference:F2})"));

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
        else
        {
            _logger.Debug("Not enough money to buy undervalued symbols");
        }

        // Sell overvalued held symbols
        foreach (var position in assets.Positions.Values.Where(p => overvalued.Any(s => s.Symbol == p.Symbol)))
        {
            actions.Add(TradingAction.MarketSell(position.Symbol, position.AvailableQuantity,
                _tradingTask.GetTaskTime()));
        }

        return actions;
    }
}
