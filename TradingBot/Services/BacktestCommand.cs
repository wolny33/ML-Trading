using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IBacktestCommand
{
    Task<Guid> CreateNewAsync(DateOnly start, DateOnly end, DateTimeOffset simulationStart,
        CancellationToken token = default);

    Task SetStateAndEndAsync(Guid backtestId, BacktestCompletionDetails details, CancellationToken token = default);
}

public sealed class BacktestCommand : IBacktestCommand
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public BacktestCommand(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Guid> CreateNewAsync(DateOnly start, DateOnly end, DateTimeOffset executionStart,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var backtest = new BacktestEntity
        {
            Id = Guid.NewGuid(),
            SimulationStart = start,
            SimulationEnd = end,
            ExecutionStartTimestamp = executionStart.ToUnixTimeMilliseconds(),
            ExecutionEndTimestamp = null,
            State = BacktestState.Running,
            StateDetails = "Backtest is running"
        };
        context.Backtests.Add(backtest);
        await context.SaveChangesAsync(token);

        return backtest.Id;
    }

    public async Task SetStateAndEndAsync(Guid backtestId, BacktestCompletionDetails details,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.Backtests.FirstOrDefaultAsync(b => b.Id == backtestId, token);
        if (entity is null)
            throw new InvalidOperationException($"There is no backtest with ID {backtestId} is database");

        if (entity.ExecutionEndTimestamp is not null)
            throw new InvalidOperationException(
                $"Backtest with ID {backtestId} has already finished with status '{entity.State.ToString()}'");

        entity.State = details.State;
        entity.StateDetails = details.StateDescription;
        entity.ExecutionEndTimestamp = details.ExecutionEnd.ToUnixTimeMilliseconds();

        await context.SaveChangesAsync(token);
    }
}

public sealed record BacktestCompletionDetails(DateTimeOffset ExecutionEnd, BacktestState State,
    string StateDescription);
