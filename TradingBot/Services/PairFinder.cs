using System.Collections.Concurrent;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IPairFinder
{
    Task<Guid> StartNewPairGroupCreationAsync(DateOnly start, DateOnly end, Guid? backtestId);
}

public sealed class PairFinder : IPairFinder, IAsyncDisposable
{
    private readonly IPairGroupCommand _pairGroupCommand;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<Guid, PairCreationTask> _tasksPerBacktest = new();
    private PairCreationTask? _pairCreationTask;

    public PairFinder(IPairGroupCommand pairGroupCommand, IServiceScopeFactory scopeFactory)
    {
        _pairGroupCommand = pairGroupCommand;
        _scopeFactory = scopeFactory;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var creationTask in _tasksPerBacktest.Values.Concat(_pairCreationTask is null
                     ? Array.Empty<PairCreationTask>()
                     : new[] { _pairCreationTask }))
        {
            await creationTask.DisposeAsync();
        }
    }

    public async Task<Guid> StartNewPairGroupCreationAsync(DateOnly start, DateOnly end, Guid? backtestId)
    {
        if (backtestId is null)
        {
            _pairCreationTask = await CreateNewTaskAsync(_pairCreationTask, start, end);
            return _pairCreationTask.PairGroupId;
        }

        var newTask =
            await CreateNewTaskAsync(_tasksPerBacktest.TryGetValue(backtestId.Value, out var oldTask) ? oldTask : null,
                start, end);
        _tasksPerBacktest[backtestId.Value] = newTask;
        return newTask.PairGroupId;
    }

    private async Task<PairCreationTask> CreateNewTaskAsync(PairCreationTask? oldTask, DateOnly start, DateOnly end)
    {
        if (oldTask is not null)
        {
            if (!oldTask.Task.IsCompleted)
            {
                throw new InvalidOperationException("There exists a conflicting pair creation task");
            }

            await oldTask.DisposeAsync();
        }

        var id = Guid.NewGuid();
        var tokenSource = new CancellationTokenSource();
        return new PairCreationTask(id, start, end, tokenSource,
            CreatePairsAndSaveAsync(id, start, end, tokenSource.Token));
    }

    private async Task CreatePairsAndSaveAsync(Guid pairGroupId, DateOnly start, DateOnly end, CancellationToken token)
    {
        await Task.Yield();
        var pairGroup = await CreatePairsAsync(pairGroupId, start, end, token);
        await _pairGroupCommand.SavePairGroupAsync(pairGroup, token);
    }

    private async Task<PairGroup> CreatePairsAsync(Guid pairGroupId, DateOnly start, DateOnly end,
        CancellationToken token = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var marketDataSource = scope.ServiceProvider.GetRequiredService<IMarketDataSource>();

        var marketData = await marketDataSource.GetPricesForAllSymbolsAsync(start, end, token);
        var normalizedPrices = GetNormalizedClosePrices(marketData);
        var scoredPairs = ScoreAllPairs(normalizedPrices);

        return new PairGroup
        {
            Id = pairGroupId,
            DeterminedAt = DateTimeOffset.Now, // TODO: Change
            Pairs = scoredPairs.OrderByDescending(pair => pair.Value).Select(pair => pair.Key).Take(5).ToList()
        };
    }

    private static Dictionary<Pair, decimal> ScoreAllPairs(
        Dictionary<TradingSymbol, IReadOnlyList<decimal>> normalizedPrices)
    {
        var scoredPairs = new Dictionary<Pair, decimal>();
        foreach (var firstSymbol in normalizedPrices.Keys)
        {
            foreach (var secondSymbol in normalizedPrices.Keys)
            {
                if (firstSymbol == secondSymbol)
                {
                    continue;
                }

                var symbolPair = Pair.CreateOrdered(firstSymbol, secondSymbol);
                if (scoredPairs.ContainsKey(symbolPair))
                {
                    continue;
                }

                scoredPairs[symbolPair] =
                    CalculatePairCorrelation(normalizedPrices[firstSymbol], normalizedPrices[secondSymbol]);
            }
        }

        return scoredPairs;
    }

    private static Dictionary<TradingSymbol, IReadOnlyList<decimal>> GetNormalizedClosePrices(
        IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>> marketData)
    {
        var normalizedPrices = new Dictionary<TradingSymbol, IReadOnlyList<decimal>>();
        foreach (var (symbol, data) in marketData)
        {
            var meanClosePrice = data.Select(d => d.Close).Sum() / data.Count;
            normalizedPrices[symbol] = data.Select(d => d.Close / meanClosePrice).ToList();
        }

        return normalizedPrices;
    }

    private static decimal CalculatePairCorrelation(IReadOnlyList<decimal> first, IReadOnlyList<decimal> second)
    {
        return first.Zip(second).Sum(pair => (pair.First - pair.Second) * (pair.First - pair.Second));
    }

    private sealed record PairCreationTask(
        Guid PairGroupId,
        DateOnly Start,
        DateOnly End,
        CancellationTokenSource TokenSource,
        Task Task) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            if (!Task.IsCompleted)
            {
                TokenSource.Cancel();
                await Task;
            }

            TokenSource.Dispose();
        }
    }
}
