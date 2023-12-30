using Microsoft.AspNetCore.Authentication;
using TradingBot.Exceptions;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IBacktestExecutor
{
    Task ExecuteAsync(DateOnly start, DateOnly end, decimal initialCash);
}

public sealed class BacktestExecutor : IBacktestExecutor
{
    private readonly IAssetsStateCommand _assetsStateCommand;
    private readonly IBacktestAssets _backtestAssets;
    private readonly IBacktestCommand _backtestCommand;
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

    public async Task ExecuteAsync(DateOnly start, DateOnly end, decimal initialCash)
    {
        var backtestId = await _backtestCommand.CreateNewAsync(start, end, _clock.UtcNow);
        _backtestAssets.InitializeForId(backtestId, initialCash);
        await _assetsStateCommand.SaveAssetsForBacktestWithIdAsync(backtestId,
            new DateTimeOffset(start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), TimeSpan.Zero));

        try
        {
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                using var scope = _scopeFactory.CreateScope();

                var task = scope.ServiceProvider.GetRequiredService<ICurrentTradingTask>();
                task.SetBacktestDetails(backtestId, day);

                var taskExecutor = scope.ServiceProvider.GetRequiredService<TradingTaskExecutor>();
                await taskExecutor.ExecuteAsync();
                await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(backtestId, day);

                await _assetsStateCommand.SaveAssetsForBacktestWithIdAsync(backtestId, task.GetTaskTime().AddHours(1));
            }

            await _backtestCommand.SetStateAndEndAsync(backtestId,
                new BacktestCompletionDetails(_clock.UtcNow, BacktestState.Finished, "Finished successfully"));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Backtest {Id} (for period from {Start} to {End}) has failed", backtestId, start, end);
            var error = (e as ResponseException)?.GetError() ?? new Error("unknown", e.Message);
            await EndWithErrorAsync(backtestId, error);
        }
    }

    private Task EndWithErrorAsync(Guid id, Error error)
    {
        return _backtestCommand.SetStateAndEndAsync(id,
            new BacktestCompletionDetails(_clock.UtcNow, BacktestState.Error,
                $"Backtest failed with error code {error.Code}: {error.Message}"));
    }
}
