using System.Collections.Concurrent;
using System.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Statistics;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public interface IPcaDecompositionCreator
{
    Task StartNewDecompositionCreationAsync(Guid? backtestId, DateOnly currentDay, CancellationToken token = default);
    Task WaitForTaskAsync(Guid? backtestId, CancellationToken token = default);
}

public sealed class PcaDecompositionCreator : IPcaDecompositionCreator, IAsyncDisposable
{
    private const int AnalysisLength = 3 * 30;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<BacktestId, PcaDecompositionTask> _tasks = new();

    public PcaDecompositionCreator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_tasks.Values.Select(async task => await task.DisposeAsync()));
    }

    public async Task StartNewDecompositionCreationAsync(Guid? backtestId, DateOnly currentDay,
        CancellationToken token = default)
    {
        if (_tasks.TryGetValue(new BacktestId(backtestId), out var task) && !task.IsCompleted) return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var marketData = await scope.ServiceProvider.GetRequiredService<IMarketDataSource>()
            .GetPricesForAllSymbolsAsync(currentDay.AddDays(-AnalysisLength), currentDay, token);

        if (task is not null) await task.DisposeAsync();

        _tasks[new BacktestId(backtestId)] =
            new PcaDecompositionTask(marketData.AsReadOnly(), backtestId, currentDay,
                scope.ServiceProvider.GetRequiredService<IPcaDecompositionService>());
    }

    public async Task WaitForTaskAsync(Guid? backtestId, CancellationToken token = default)
    {
        if (!_tasks.TryGetValue(new BacktestId(backtestId), out var task) || task.IsCompleted) return;

        token.Register(() => task.Cancel());
        await task.Task;
    }

    private sealed record BacktestId(Guid? Value);
}

public sealed class PcaDecompositionTask : IAsyncDisposable
{
    private const double VarianceFraction = 0.9;
    private const int DecompositionExpiration = 7;

    private readonly Guid? _backtestId;
    private readonly DateOnly _date;
    private readonly IPcaDecompositionService _decompositionService;
    private readonly IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> _marketData;
    private readonly CancellationTokenSource _tokenSource = new();

    public PcaDecompositionTask(IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> marketData,
        Guid? backtestId, DateOnly date, IPcaDecompositionService decompositionService)
    {
        _marketData = marketData;
        _backtestId = backtestId;
        _date = date;
        _decompositionService = decompositionService;

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
        var decomposition = CreateDecomposition(token);
        await _decompositionService.SaveDecompositionAsync(decomposition, _backtestId, token);
    }

    private PcaDecomposition CreateDecomposition(CancellationToken token)
    {
        var (symbols, dataLength) = GetDecompositionSymbols(_marketData);

        var priceMatrix =
            DenseMatrix.OfColumnArrays(symbols.Select(s =>
                _marketData[s].Select(data => (double)data.Close).ToArray()));

        var means = priceMatrix.ColumnSums() / dataLength;
        var stdDevs = DenseVector.OfEnumerable(priceMatrix.EnumerateColumns().Select(c => c.StandardDeviation()));

        var standardizedMatrix =
            DenseMatrix.OfColumnVectors(
                priceMatrix.EnumerateColumns().Select((col, i) => (col - means[i]) / stdDevs[i]));

        var covarianceMatrix = standardizedMatrix.Transpose() * standardizedMatrix / (dataLength - 1);

        var evd = covarianceMatrix.Evd();

        var selectedEigenVectors = ReduceComponents(evd.EigenVectors, evd.EigenValues.Real().ToList());

        return new PcaDecomposition
        {
            CreatedAt = _date,
            ExpiresAt = _date.AddDays(DecompositionExpiration),
            Symbols = symbols,
            Means = means,
            StandardDeviations = stdDevs,
            PrincipalVectors = selectedEigenVectors
        };
    }

    private static SymbolsAndDataLength GetDecompositionSymbols(
        IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> marketData)
    {
        var longestLength = marketData.Values.Max(data => data.Count);
        var validSymbols = marketData.Keys
            .Where(symbol => marketData[symbol].Count >= longestLength)
            .OrderBy(symbol => symbol.Value)
            .ToList();

        if (validSymbols.Count < 0.5 * marketData.Keys.Count())
            // TODO: Use logger instead
            throw new UnreachableException("A lot of symbols have missing data");

        return new SymbolsAndDataLength(validSymbols, longestLength);
    }

    private static Matrix<double> ReduceComponents(Matrix<double> eigenVectors, IReadOnlyList<double> eigenValues)
    {
        var sortedIndices = eigenValues.Select((value, index) => new { Value = value, Index = index })
            .OrderByDescending(x => x.Value)
            .Select(x => x.Index);

        var selectedIndices = new List<int>();
        var totalVariance = eigenValues.Sum();
        var accumulatedVariance = 0.0;
        foreach (var index in sortedIndices)
        {
            if (accumulatedVariance >= totalVariance * VarianceFraction) break;

            selectedIndices.Add(index);
            accumulatedVariance += eigenValues[index];
        }

        return DenseMatrix.OfColumnVectors(selectedIndices.Select(eigenVectors.Column));
    }

    public void Cancel()
    {
        if (!Task.IsCompleted) _tokenSource.Cancel();
    }

    private sealed record SymbolsAndDataLength(IReadOnlyList<TradingSymbol> Symbols, int DataLength);
}
