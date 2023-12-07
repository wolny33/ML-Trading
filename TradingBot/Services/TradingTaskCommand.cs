using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;

namespace TradingBot.Services;

public interface ITradingTaskCommand
{
    Task<Guid> CreateNewAsync(DateTimeOffset start, CancellationToken token = default);
    Task SetStateAndEndAsync(Guid taskId, TradingTaskCompletionDetails details, CancellationToken token = default);
}

public sealed class TradingTaskCommand : ITradingTaskCommand
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingTaskCommand(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Guid> CreateNewAsync(DateTimeOffset start, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var newTask = new TradingTaskEntity
        {
            Id = Guid.NewGuid(),
            StartTimestamp = start.ToUnixTimeMilliseconds(),
            State = TradingTaskState.Running,
            StateDetails = "Trading task is running",
            EndTimestamp = null
        };
        context.TradingTasks.Add(newTask);
        await context.SaveChangesAsync(token);

        return newTask.Id;
    }

    public async Task SetStateAndEndAsync(Guid taskId, TradingTaskCompletionDetails details,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TradingTasks.FirstOrDefaultAsync(t => t.Id == taskId, token);
        if (entity is null)
            throw new InvalidOperationException($"There is no trading task with ID {taskId} is database");

        if (entity.EndTimestamp is not null)
            throw new InvalidOperationException(
                $"Trading task with ID {taskId} was already finished with status '{entity.State.ToString()}'");

        entity.State = details.State;
        entity.StateDetails = details.StateDescription;
        entity.EndTimestamp = details.End.ToUnixTimeMilliseconds();

        await context.SaveChangesAsync(token);
    }
}

public sealed record TradingTaskCompletionDetails(DateTimeOffset End, TradingTaskState State, string StateDescription);
