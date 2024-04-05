using System.Collections.Concurrent;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Statistics;
using TradingBot.Configuration;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services.Strategy;

public interface IPcaDecompositionCreator
{
    Task StartNewDecompositionCreationAsync(Guid? backtestId, DateOnly currentDay, PcaOptions options,
        BacktestSymbolSlice slice, CancellationToken token = default);

    Task WaitForTaskAsync(Guid? backtestId, CancellationToken token = default);
}

public sealed class PcaDecompositionCreator : IPcaDecompositionCreator, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<BacktestId, PcaDecompositionTask> _tasks = new();

    public PcaDecompositionCreator(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger.ForContext<PcaDecompositionCreator>();
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_tasks.Values.Select(async task => await task.DisposeAsync()));
    }

    public async Task StartNewDecompositionCreationAsync(Guid? backtestId, DateOnly currentDay, PcaOptions options,
        BacktestSymbolSlice slice, CancellationToken token = default)
    {
        if (_tasks.TryGetValue(new BacktestId(backtestId), out var task) && !task.IsCompleted)
        {
            _logger.Debug("There is a running PCA task for backtest {BacktestId} - skipping creation request",
                backtestId);
            return;
        }

        _logger.Debug("Creating new PCA decomposition for backtest {BacktestId} for day {Day}", backtestId, currentDay);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var marketData = await scope.ServiceProvider.GetRequiredService<IMarketDataSource>()
            .GetPricesForAllSymbolsAsync(currentDay.AddDays(-options.AnalysisLengthInDays), currentDay, slice, token);

        if (task is not null) await task.DisposeAsync();

        _tasks[new BacktestId(backtestId)] = new PcaDecompositionTask(marketData.AsReadOnly(), backtestId, currentDay,
            scope.ServiceProvider.GetRequiredService<IPcaDecompositionService>(), _logger, options);

        _logger.Debug("PCA task for backtest {BacktestId} for day {Day} was started", backtestId, currentDay);
    }

    public async Task WaitForTaskAsync(Guid? backtestId, CancellationToken token = default)
    {
        if (!_tasks.TryGetValue(new BacktestId(backtestId), out var task) || task.IsCompleted)
        {
            _logger.Verbose("There is no task to wait for (backtest {BacktestId})", backtestId);
            return;
        }

        token.Register(() =>
        {
            _logger.Debug(
                "Backtest cancellation was requested before PCA task could finish - cancelling task (backtest {BacktestId})",
                backtestId);
            task.Cancel();
        });
        await task.Task;
    }

    private sealed record BacktestId(Guid? Value);
}

public sealed class PcaDecompositionTask : IAsyncDisposable
{
    private readonly Guid? _backtestId;
    private readonly DateOnly _date;

    private readonly IPcaDecompositionService _decompositionService;
    private readonly ILogger _logger;
    private readonly IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> _marketData;
    private readonly PcaOptions _options;
    private readonly CancellationTokenSource _tokenSource = new();

    public PcaDecompositionTask(IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> marketData,
        Guid? backtestId, DateOnly date, IPcaDecompositionService decompositionService, ILogger logger,
        PcaOptions options)
    {
        _marketData = marketData;
        _backtestId = backtestId;
        _date = date;
        _decompositionService = decompositionService;
        _options = options;
        _logger = logger.ForContext<PcaDecompositionTask>();

        Task = CreateAndSaveNewDecompositionAsync(_tokenSource.Token);
    }

    public bool IsCompleted => Task.IsCompleted;
    public Task Task { get; }

    public async ValueTask DisposeAsync()
    {
        Cancel();
        await Task;
        _tokenSource.Dispose();
    }

    private async Task CreateAndSaveNewDecompositionAsync(CancellationToken token)
    {
        await Task.Yield();
        _logger.Debug("({BacktestId}: {Day}) Starting PCA task", _backtestId, _date);
        var decomposition = await Task.Run(() => CreateDecomposition(token), token);
        _logger.Debug("({BacktestId}: {Day}) PCA decomposition created - saving into db", _backtestId, _date);
        await _decompositionService.SaveDecompositionAsync(decomposition, _backtestId, token);
    }

    private PcaDecomposition CreateDecomposition(CancellationToken token)
    {
        var (symbols, dataLength) = GetDecompositionSymbols(_marketData);

        if (symbols.Count == 0 || dataLength < 2)
        {
            _logger.Warning(
                "({BacktestId}: {Day}) Not enough data to create decomposition: {Symbols} symbols with {Days} days",
                _backtestId, _date, symbols.Count, dataLength);
            return CreateEmptyDecomposition(symbols.Count > 0 ? symbols : new[] { new TradingSymbol("None") });
        }

        _logger.Debug("({BacktestId}: {Day}) Decomposition will include {Count} symbols (data has length {Length})",
            _backtestId, _date, symbols.Count, dataLength);
        _logger.Verbose("Symbols: {Symbols}", symbols.Select(s => s.Value));

        var priceMatrix =
            DenseMatrix.OfColumnArrays(symbols.Select(s =>
                _marketData[s].Select(data => (double)data.Close).ToArray()));

        var means = priceMatrix.ColumnSums() / dataLength;
        var stdDevs = DenseVector.OfEnumerable(priceMatrix.EnumerateColumns().Select(c => c.StandardDeviation()));

        var standardizedMatrix =
            DenseMatrix.OfColumnVectors(
                priceMatrix.EnumerateColumns().Select((col, i) => (col - means[i]) / stdDevs[i]));

        _logger.Verbose("({BacktestId}: {Day}) Creating covariance matrix", _backtestId, _date);
        var covarianceMatrix = standardizedMatrix.Transpose() * standardizedMatrix / (dataLength - 1);

        _logger.Verbose("({BacktestId}: {Day}) Finding eigenvectors", _backtestId, _date);
        var evd = covarianceMatrix.Evd();

        _logger.Verbose("({BacktestId}: {Day}) Determining most important components", _backtestId, _date);
        var selectedEigenVectors = ReduceComponents(evd.EigenVectors, evd.EigenValues.Real().ToList());

        _logger.Verbose("({BacktestId}: {Day}) Creating transformation matrix", _backtestId, _date);
        var projectionMatrix = selectedEigenVectors * selectedEigenVectors.Transpose();

        _logger.Verbose("({BacktestId}: {Day}) Calculating norms", _backtestId, _date);
        var l1Norms = DenseVector.OfEnumerable(projectionMatrix.EnumerateColumns().Select(col => col.L1Norm()));
        var l2Norms = DenseVector.OfEnumerable(projectionMatrix.EnumerateColumns().Select(col => col.L2Norm()));

        return new PcaDecomposition
        {
            CreatedAt = _date,
            ExpiresAt = _date.AddDays(_options.DecompositionExpirationInDays),
            Symbols = symbols,
            Means = means,
            StandardDeviations = stdDevs,
            L1Norms = l1Norms,
            L2Norms = l2Norms,
            PrincipalVectors = selectedEigenVectors
        };
    }

    private SymbolsAndDataLength GetDecompositionSymbols(
        IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> marketData)
    {
        if (marketData.Count == 0)
        {
            return new SymbolsAndDataLength(Array.Empty<TradingSymbol>(), 0);
        }

        var longestLength = marketData.Values.Max(data => data.Count);
        var validSymbols = marketData.Keys
            .Where(symbol => marketData[symbol].Count >= longestLength)
            .OrderBy(symbol => symbol.Value)
            .ToList();

        if (validSymbols.Count < 0.5 * marketData.Keys.Count())
        {
            _logger.Warning(
                "({BacktestId}: {Day}) A lot of symbols has missing data - {Valid} out of {All} (distinct data lengths: {Lengths})",
                _backtestId, _date, validSymbols.Count, marketData.Keys.Count(),
                marketData.Values.Select(data => data.Count).Distinct().OrderDescending());
        }
        else
        {
            _logger.Verbose("({BacktestId}: {Day}) {Valid} of {All} symbols didn't have missing data", _backtestId,
                _date, validSymbols.Count, marketData.Keys.Count());
        }

        return new SymbolsAndDataLength(validSymbols, longestLength);
    }

    private Matrix<double> ReduceComponents(Matrix<double> eigenVectors, IReadOnlyList<double> eigenValues)
    {
        var sortedIndices = eigenValues.Select((value, index) => new { Value = value, Index = index })
            .OrderByDescending(x => x.Value)
            .Select(x => x.Index);

        var selectedIndices = new List<int>();
        var totalVariance = eigenValues.Sum();
        var accumulatedVariance = 0.0;
        foreach (var index in sortedIndices)
        {
            if (accumulatedVariance >= totalVariance * _options.VarianceFraction) break;

            selectedIndices.Add(index);
            accumulatedVariance += eigenValues[index];
        }

        _logger.Debug(
            "({BacktestId}: {Day}) Selected {Count} out of {All} components, accounting for {Percentage}% of variance",
            _backtestId, _date, selectedIndices.Count, eigenValues.Count, accumulatedVariance / totalVariance * 100);

        return DenseMatrix.OfColumnVectors(selectedIndices.Select(eigenVectors.Column));
    }

    private PcaDecomposition CreateEmptyDecomposition(IReadOnlyList<TradingSymbol> symbols)
    {
        return new PcaDecomposition
        {
            CreatedAt = _date,
            ExpiresAt = _date,
            Symbols = symbols,
            Means = DenseVector.OfEnumerable(Enumerable.Repeat(0.0, symbols.Count)),
            StandardDeviations = DenseVector.OfEnumerable(Enumerable.Repeat(1.0, symbols.Count)),
            L1Norms = DenseVector.OfEnumerable(Enumerable.Repeat(1.0, symbols.Count)),
            L2Norms = DenseVector.OfEnumerable(Enumerable.Repeat(1.0, symbols.Count)),
            PrincipalVectors = DenseMatrix.OfDiagonalArray(Enumerable.Repeat(1.0, symbols.Count).ToArray())
        };
    }

    public void Cancel()
    {
        if (Task.IsCompleted) return;
        _logger.Debug("({BacktestId}: {Day}) PCA task was cancelled", _backtestId, _date);
        _tokenSource.Cancel();
    }

    private sealed record SymbolsAndDataLength(IReadOnlyList<TradingSymbol> Symbols, int DataLength);
}
