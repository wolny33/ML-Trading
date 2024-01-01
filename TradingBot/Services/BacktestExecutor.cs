﻿using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authentication;
using TradingBot.Exceptions;
using TradingBot.Models;
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

    private readonly ConcurrentDictionary<Guid, RunningBacktest> _backtests = new();
    private readonly ISystemClock _clock;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BacktestExecutor(IServiceScopeFactory scopeFactory, ILogger logger, IBacktestCommand backtestCommand,
        ISystemClock clock, IBacktestAssets backtestAssets, IAssetsStateCommand assetsStateCommand)
    {
        _scopeFactory = scopeFactory;
        _backtestCommand = backtestCommand;
        _clock = clock;
        _backtestAssets = backtestAssets;
        _assetsStateCommand = assetsStateCommand;
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
        var backtestId = await _backtestCommand.CreateNewAsync(details.Start, details.End, _clock.UtcNow, id, token);
        _logger.Information("Started new backtest with ID {Id}, from {Start} to {End}", backtestId, details.Start,
            details.End);

        _backtestAssets.InitializeForId(backtestId, details.InitialCash);
        await _assetsStateCommand.SaveAssetsForBacktestWithIdAsync(backtestId,
            new DateTimeOffset(details.Start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), TimeSpan.Zero), token);

        try
        {
            using var initializationScope = _scopeFactory.CreateScope();
            var marketDataSource = initializationScope.ServiceProvider.GetRequiredService<IMarketDataSource>();
            await marketDataSource.InitializeBacktestDataAsync(details.Start.AddDays(-10), details.End, token);

            for (var day = details.Start; day < details.End; day = day.AddDays(1))
            {
                if (token.IsCancellationRequested) await MarkAsCancelledAsync(backtestId);

                _logger.Debug("Executing day {Day} for backtest {Id}", day, backtestId);

                using var scope = _scopeFactory.CreateScope();
                var task = scope.ServiceProvider.GetRequiredService<ICurrentTradingTask>();
                task.SetBacktestDetails(backtestId, day);

                var taskExecutor = scope.ServiceProvider.GetRequiredService<TradingTaskExecutor>();
                await taskExecutor.ExecuteAsync(token);
                await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(backtestId, day.AddDays(1), token);

                await _assetsStateCommand.SaveAssetsForBacktestWithIdAsync(backtestId,
                    task.GetTaskTime().AddDays(1).AddHours(-1),
                    token);
            }

            _logger.Information("Backtest {Id} finished successfully", backtestId);
            await _backtestCommand.SetStateAndEndAsync(backtestId,
                new BacktestCompletionDetails(_clock.UtcNow, BacktestState.Finished, "Finished successfully"),
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
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

public sealed record BacktestDetails(DateOnly Start, DateOnly End, decimal InitialCash);
