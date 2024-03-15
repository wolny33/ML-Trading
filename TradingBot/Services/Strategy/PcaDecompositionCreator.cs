using System.Collections.Concurrent;
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
            new PcaDecompositionTask(marketData.AsReadOnly(), backtestId,
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
    private readonly Guid? _backtestId;
    private readonly IPcaDecompositionService _decompositionService;
    private readonly IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> _marketData;
    private readonly CancellationTokenSource _tokenSource = new();

    public PcaDecompositionTask(IReadOnlyDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> marketData,
        Guid? backtestId, IPcaDecompositionService decompositionService)
    {
        _marketData = marketData;
        _backtestId = backtestId;
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
        var decomposition = await CreateDecompositionAsync(token);
        await _decompositionService.SaveDecompositionAsync(decomposition, _backtestId, token);
    }

    private Task<PcaDecomposition> CreateDecompositionAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public void Cancel()
    {
        if (!Task.IsCompleted) _tokenSource.Cancel();
    }
}
