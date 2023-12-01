using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;

namespace TradingBot.Services;

public interface ITradingActionCommand
{
    Task SaveActionWithAlpacaIdAsync(TradingAction action, Guid alpacaId, CancellationToken token = default);
}

public sealed class TradingActionCommand : ITradingActionCommand
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingActionCommand(IDbContextFactory<AppDbContext> dbContextFactory, IAlpacaClientFactory clientFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SaveActionWithAlpacaIdAsync(TradingAction action, Guid alpacaId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);

        var entity = action.ToEntity();
        entity.AlpacaId = alpacaId;

        context.TradingActions.Add(entity);
        await context.SaveChangesAsync(token);
    }
}
