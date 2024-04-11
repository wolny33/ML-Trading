using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using TradingBot.Exceptions;
using TradingBot.Models;
using TradingBot.Services.Strategy;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IBacktestExecutor
{
    Guid StartNew(BacktestDetails details);
    Task CancelBacktestAsync(Guid id);
}

public sealed class BacktestExecutor : IBacktestExecutor, IAsyncDisposable
{
    private readonly IAssetsStateCommand _assetsStateCommand;
    private readonly IBacktestAssets _backtestAssets;
    private readonly IBacktestCommand _backtestCommand;
    private readonly IPcaDecompositionCreator _decompositionCreator;

    private readonly ConcurrentDictionary<Guid, RunningBacktest> _backtests = new();
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BacktestExecutor(IServiceScopeFactory scopeFactory, ILogger logger, IBacktestCommand backtestCommand,
        ISystemClock clock, IBacktestAssets backtestAssets, IAssetsStateCommand assetsStateCommand,
        IPcaDecompositionCreator decompositionCreator)
    {
        _scopeFactory = scopeFactory;
        _backtestCommand = backtestCommand;
        _clock = clock;
        _backtestAssets = backtestAssets;
        _assetsStateCommand = assetsStateCommand;
        _decompositionCreator = decompositionCreator;
        _logger = logger.ForContext<BacktestExecutor>();
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_backtests.Values.Select(async b => await b.DisposeAsync()));
    }

    public Guid StartNew(BacktestDetails details)
    {
        var id = Guid.NewGuid();
        var tokenSource = new CancellationTokenSource();
        _backtests[id] = new RunningBacktest(ExecuteAsync(details, id, tokenSource.Token), tokenSource);

        return id;
    }

    public async Task CancelBacktestAsync(Guid id)
    {
        if (!_backtests.Remove(id, out var backtest)) return;

        await backtest.DisposeAsync();
    }

    private async Task ExecuteAsync(BacktestDetails details, Guid id, CancellationToken token = default)
    {
        var backtestId = await _backtestCommand.CreateNewAsync(
            new BacktestCreationDetails(id, details.Start, details.End, _clock.UtcNow, details.Predictor,
                details.Description),
            token);
        _logger.Information("Started new backtest with ID {Id}, from {Start} to {End}", backtestId, details.Start,
            details.End);

        _backtestAssets.InitializeForId(backtestId, details.InitialCash);
        await _assetsStateCommand.SaveAssetsForBacktestWithIdAsync(backtestId,
            new DateTimeOffset(details.Start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), TimeSpan.Zero), token);

        try
        {
            await Task.Yield();
            await using var initializationScope = _scopeFactory.CreateAsyncScope();
            var marketDataSource = initializationScope.ServiceProvider.GetRequiredService<IMarketDataSource>();
            var strategy = await initializationScope.ServiceProvider.GetRequiredService<IStrategyFactory>()
                .CreateAsync(token);

            // Predictor needs 10 valid days (excluding weekends and holidays) before start, so 20 days should be enough
            // If strategy needs more data, we get more data
            await marketDataSource.InitializeBacktestDataAsync(
                details.Start.AddDays(-int.Max(20, await strategy.GetRequiredPastDaysAsync(token) + 1)),
                details.Predictor.UsePredictor
                    ? details.End
                    : details.End.AddDays(2 * PricePredictor.PredictorOutputLength),
                details.SymbolSlice, backtestId, token);

            for (var day = details.Start; day < details.End; day = day.AddDays(1))
            {
                if (token.IsCancellationRequested) await MarkAsCancelledAsync(backtestId);

                _logger.Debug("Executing day {Day} for backtest {Id}", day, backtestId);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var task = scope.ServiceProvider.GetRequiredService<ICurrentTradingTask>();
                task.SetBacktestDetails(backtestId, day, details.SymbolSlice, details.Predictor);

                var taskExecutor = scope.ServiceProvider.GetRequiredService<TradingTaskExecutor>();
                await taskExecutor.ExecuteAsync(token);
                await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(backtestId, day.AddDays(1), token);

                await _assetsStateCommand.SaveAssetsForBacktestWithIdAsync(backtestId,
                    task.GetTaskTime().AddDays(1).AddHours(-1),
                    token);

                await DoEndOfDayActionsAsync(backtestId, token);
            }

            _logger.Information("Backtest {Id} finished successfully", backtestId);
            await _backtestCommand.SetStateAndEndAsync(backtestId,
                new BacktestCompletionDetails(_clock.UtcNow, BacktestState.Finished, "Finished successfully"),
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Backtest {Id} was cancelled", backtestId);
            await MarkAsCancelledAsync(backtestId);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Backtest {Id} (from {Start} to {End}) has failed", backtestId, details.Start,
                details.End);
            await EndWithErrorAsync(backtestId, e);
        }
    }

    private Task EndWithErrorAsync(Guid id, Exception exception)
    {
        var error = (exception as ResponseException)?.GetError() ?? new Error("unknown", exception.Message);
        return _backtestCommand.SetStateAndEndAsync(id,
            new BacktestCompletionDetails(_clock.UtcNow, BacktestState.Error,
                $"Backtest failed with error code {error.Code}: {error.Message}"), CancellationToken.None);
    }

    private Task MarkAsCancelledAsync(Guid id)
    {
        return _backtestCommand.SetStateAndEndAsync(id,
            new BacktestCompletionDetails(_clock.UtcNow, BacktestState.Cancelled, "Backtest was cancelled"),
            CancellationToken.None);
    }

    /// <summary>
    ///     Performs actions that would normally happen in between days - i.e. waits for long operations to finish
    /// </summary>
    private async Task DoEndOfDayActionsAsync(Guid id, CancellationToken token)
    {
        await _decompositionCreator.WaitForTaskAsync(id, token);
    }

    private sealed record RunningBacktest(Task BacktestTask, CancellationTokenSource TokenSource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            if (!BacktestTask.IsCompleted)
            {
                TokenSource.Cancel();
                await BacktestTask;
            }

            TokenSource.Dispose();
        }
    }
}

public sealed record BacktestDetails(
    DateOnly Start,
    DateOnly End,
    BacktestSymbolSlice SymbolSlice,
    decimal InitialCash,
    BacktestPredictorConfiguration Predictor,
    string Description);

public sealed record BacktestSymbolSlice(int Skip, int Take);

public sealed record BacktestPredictorConfiguration(bool UsePredictor, double MeanError);
