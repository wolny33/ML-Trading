using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Exceptions;
using TradingBot.Models;

namespace TradingBot.Services;

public interface ITradingActionCommand
{
    Task SaveActionWithAlpacaIdAsync(TradingAction action, Guid alpacaId, Guid? taskId,
        CancellationToken token = default);

    Task SaveActionWithErrorAsync(TradingAction action, Error error, Guid? taskId, CancellationToken token = default);
}

public sealed class TradingActionCommand : ITradingActionCommand
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingActionCommand(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SaveActionWithAlpacaIdAsync(TradingAction action, Guid alpacaId, Guid? taskId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);

        var entity = action.ToEntity();
        entity.AlpacaId = alpacaId;
        entity.TradingTaskId = taskId;

        context.TradingActions.Add(entity);
        await context.SaveChangesAsync(token);
    }

    public async Task SaveActionWithErrorAsync(TradingAction action, Error error, Guid? taskId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);

        var entity = action.ToEntity();
        entity.ErrorCode = error.Code;
        entity.ErrorMessage = error.Message;
        entity.TradingTaskId = taskId;

        context.TradingActions.Add(entity);
        await context.SaveChangesAsync(token);
    }
}
