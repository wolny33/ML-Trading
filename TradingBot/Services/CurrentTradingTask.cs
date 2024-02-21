using Microsoft.AspNetCore.Authentication;
using TradingBot.Exceptions;
using TradingBot.Models;

namespace TradingBot.Services;

/// <summary>
///     Handles saving trading task details, as well as linking them together
/// </summary>
/// <remarks>
///     This service must be registered as 'scoped' to correctly link details
/// </remarks>
public interface ICurrentTradingTask
{
    Guid? CurrentBacktestId { get; }
    bool ShouldReturnFutureDataFromPredictor { get; }
    Task StartAsync(CancellationToken token = default);
    Task SaveAndLinkSuccessfulActionAsync(TradingAction action, Guid alpacaId, CancellationToken token = default);
    Task SaveAndLinkBacktestActionAsync(TradingAction action, CancellationToken token = default);
    Task SaveAndLinkErroredActionAsync(TradingAction action, Error error, CancellationToken token = default);
    Task FinishSuccessfullyAsync(CancellationToken token = default);
    Task MarkAsDisabledFromConfigAsync(CancellationToken token = default);
    Task MarkAsExchangeClosedAsync(CancellationToken token = default);
    Task MarkAsErroredAsync(Error error, CancellationToken token = default);
    DateOnly GetTaskDay();
    DateTimeOffset GetTaskTime();
    void SetBacktestDetails(Guid backtestId, DateOnly day, bool usePredictor);
}

public sealed class CurrentTradingTask : ICurrentTradingTask
{
    private readonly ISystemClock _clock;
    private readonly ITradingTaskCommand _taskCommand;
    private readonly ITestModeConfigService _testModeConfig;
    private readonly ITradingActionCommand _tradingActionCommand;
    private BacktestDetails? _backtestDetails;
    private Guid? _currentTradingTaskId;

    public CurrentTradingTask(ITradingActionCommand tradingActionCommand, ITradingTaskCommand taskCommand,
        ISystemClock clock, ITestModeConfigService testModeConfig)
    {
        _tradingActionCommand = tradingActionCommand;
        _taskCommand = taskCommand;
        _clock = clock;
        _testModeConfig = testModeConfig;
    }

    public Guid? CurrentBacktestId => _backtestDetails?.Id;

    public bool ShouldReturnFutureDataFromPredictor => _backtestDetails?.UsePredictor is false;

    public async Task StartAsync(CancellationToken token = default)
    {
        var mode = CurrentBacktestId is not null ? Mode.Backtest : await _testModeConfig.GetCurrentModeAsync(token);
        _currentTradingTaskId = await _taskCommand.CreateNewAsync(GetTaskTime(), mode, CurrentBacktestId, token);
    }

    public Task SaveAndLinkSuccessfulActionAsync(TradingAction action, Guid alpacaId, CancellationToken token = default)
    {
        return _tradingActionCommand.SaveActionWithAlpacaIdAsync(action, alpacaId, _currentTradingTaskId, token);
    }

    public Task SaveAndLinkBacktestActionAsync(TradingAction action, CancellationToken token = default)
    {
        if (CurrentBacktestId is null)
            throw new InvalidOperationException(
                "Backtest trading actions cannot be saved if trading task is not part of a backtest");

        return _tradingActionCommand.SaveSuccessfulBacktestActionAsync(action, _currentTradingTaskId, token);
    }

    public Task SaveAndLinkErroredActionAsync(TradingAction action, Error error, CancellationToken token = default)
    {
        return _tradingActionCommand.SaveActionWithErrorAsync(action, error, _currentTradingTaskId, token);
    }

    public Task FinishSuccessfullyAsync(CancellationToken token = default)
    {
        return EndWithStateAsync(TradingTaskState.Success, "Finished successfully", token);
    }

    public Task MarkAsDisabledFromConfigAsync(CancellationToken token = default)
    {
        return EndWithStateAsync(TradingTaskState.ConfigDisabled, "Automatic investing is disabled in configuration",
            token);
    }

    public Task MarkAsExchangeClosedAsync(CancellationToken token = default)
    {
        return EndWithStateAsync(TradingTaskState.ExchangeClosed, "Exchange will not open in the following 24 hours",
            token);
    }

    public Task MarkAsErroredAsync(Error error, CancellationToken token = default)
    {
        return EndWithStateAsync(TradingTaskState.Error,
            $"Trading task failed with error code {error.Code}: {error.Message}", token);
    }

    public DateOnly GetTaskDay()
    {
        return _backtestDetails?.Day ?? DateOnly.FromDateTime(_clock.UtcNow.LocalDateTime);
    }

    public DateTimeOffset GetTaskTime()
    {
        // Since we use today's market data, backtest trading task is executed after trading hours
        return CurrentBacktestId is not null
            ? new DateTimeOffset(GetTaskDay().ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(20))),
                TimeSpan.Zero)
            : _clock.UtcNow;
    }

    public void SetBacktestDetails(Guid backtestId, DateOnly day, bool usePredictor)
    {
        _backtestDetails = new BacktestDetails(backtestId, day, usePredictor);
    }

    private Task EndWithStateAsync(TradingTaskState state, string description, CancellationToken token)
    {
        if (_currentTradingTaskId is not { } taskId)
            throw new InvalidOperationException("Trading task must be started before it can be completed");

        return _taskCommand.SetStateAndEndAsync(taskId,
            new TradingTaskCompletionDetails(GetTaskTime(), state, description), token);
    }

    private sealed record BacktestDetails(Guid Id, DateOnly Day, bool UsePredictor);
}
