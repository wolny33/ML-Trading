using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services.Strategy;

public abstract class PcaStrategyBase : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IPcaDecompositionCreator _decompositionCreator;
    private readonly IPcaDecompositionService _decompositionService;
    private readonly ILogger _logger;
    private readonly IMarketDataSource _marketDataSource;
    private readonly IStrategyParametersService _strategyParameters;
    private readonly ICurrentTradingTask _tradingTask;

    protected PcaStrategyBase(IPcaDecompositionService decompositionService,
        IPcaDecompositionCreator decompositionCreator,
        IAssetsDataSource assetsDataSource, IMarketDataSource marketDataSource, ICurrentTradingTask tradingTask,
        ILogger logger, IStrategyParametersService strategyParameters)
    {
        _decompositionService = decompositionService;
        _decompositionCreator = decompositionCreator;
        _assetsDataSource = assetsDataSource;
        _marketDataSource = marketDataSource;
        _tradingTask = tradingTask;
        _strategyParameters = strategyParameters;
        _logger = logger.ForContext<PcaStrategyBase>();
    }

    public abstract string Name { get; }

    public async Task<int> GetRequiredPastDaysAsync(CancellationToken token = default)
    {
        return (await _strategyParameters.GetConfigurationAsync(token)).Pca.AnalysisLengthInDays;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        var latestDecomposition =
            await _decompositionService.GetLatestDecompositionAsync(_tradingTask.CurrentBacktestId, token);
        var config = await _strategyParameters.GetConfigurationAsync(token);

        // If no decomposition was created or if decomposition expired earlier than 3 days ago, we start the task and do
        // nothing (since it may take a long time)
        if (latestDecomposition is null || latestDecomposition.ExpiresAt.AddDays(3) < _tradingTask.GetTaskDay())
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
                _tradingTask.GetTaskDay(), config.Pca, token);
            return Array.Empty<TradingAction>();
        }

        // If decomposition expired, start new creation task
        if (latestDecomposition.ExpiresAt < _tradingTask.GetTaskDay())
        {
            _logger.Debug("Latest decomposition will expire in less than 3 days (on {Expiration})",
                latestDecomposition.ExpiresAt);
            await _decompositionCreator.StartNewDecompositionCreationAsync(_tradingTask.CurrentBacktestId,
                _tradingTask.GetTaskDay(), config.Pca, token);
        }

        var lastDayData =
            await _marketDataSource.GetPricesForAllSymbolsAsync(_tradingTask.GetTaskDay(), _tradingTask.GetTaskDay(),
                token);
        var lastPrices = lastDayData.Keys.ToDictionary(symbol => symbol, symbol => lastDayData[symbol][0].Close);
        var differences = latestDecomposition.CalculatePriceDifferences(lastPrices);

        var undervalued = differences.Where(d => d.NormalizedDifference < -config.Pca.UndervaluedThreshold).ToList();
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
            actions.AddRange(await GetBuyActionsAsync(undervalued, assets, lastPrices, config.LimitPriceDamping,
                token));
        }
        else
        {
            _logger.Debug("Not enough money to buy undervalued symbols");
        }

        // Sell overvalued held symbols
        actions.AddRange(await GetSellActionsAsync(
            assets.Positions.Values.Where(p => overvalued.Any(s => s.Symbol == p.Symbol)), config.LimitPriceDamping,
            token));

        return actions;
    }

    protected abstract Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(
        IReadOnlyList<SymbolWithNormalizedDifference> undervalued, Assets assets,
        IReadOnlyDictionary<TradingSymbol, decimal> lastPrices, decimal damping, CancellationToken token);

    protected abstract Task<IReadOnlyList<TradingAction>> GetSellActionsAsync(IEnumerable<Position> positions,
        decimal damping, CancellationToken token);
}

public sealed class PcaStrategy : PcaStrategyBase
{
    private readonly ICurrentTradingTask _tradingTask;

    public PcaStrategy(IPcaDecompositionService decompositionService, IPcaDecompositionCreator decompositionCreator,
        IAssetsDataSource assetsDataSource, IMarketDataSource marketDataSource, ICurrentTradingTask tradingTask,
        ILogger logger, IStrategyParametersService strategyParameters)
        : base(decompositionService, decompositionCreator, assetsDataSource, marketDataSource, tradingTask, logger,
            strategyParameters)
    {
        _tradingTask = tradingTask;
    }

    public static string StrategyName => "PCA strategy";
    public override string Name => StrategyName;

    protected override Task<IReadOnlyList<TradingAction>> GetBuyActionsAsync(
        IReadOnlyList<SymbolWithNormalizedDifference> undervalued, Assets assets,
        IReadOnlyDictionary<TradingSymbol, decimal> lastPrices, decimal damping, CancellationToken token)
    {
        var actions = new List<TradingAction>();
        var availableMoney = assets.Cash.AvailableAmount * 0.95m;
        var sumOfDifferences = undervalued.Sum(pair => pair.NormalizedDifference);
        foreach (var (symbol, difference) in undervalued)
        {
            var investmentValue = availableMoney * (decimal)(difference / sumOfDifferences);
            actions.Add(TradingAction.MarketBuy(symbol, investmentValue / lastPrices[symbol],
                _tradingTask.GetTaskTime()));
        }

        return Task.FromResult<IReadOnlyList<TradingAction>>(actions);
    }

    protected override Task<IReadOnlyList<TradingAction>> GetSellActionsAsync(IEnumerable<Position> positions,
        decimal damping, CancellationToken token)
    {
        return Task.FromResult<IReadOnlyList<TradingAction>>(positions.Select(p =>
            TradingAction.MarketSell(p.Symbol, p.AvailableQuantity, _tradingTask.GetTaskTime())).ToList());
    }
}
