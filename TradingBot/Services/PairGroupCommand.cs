using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IPairGroupCommand
{
    Task SavePairGroupAsync(PairGroup pairGroup, CancellationToken token = default);
}

public sealed class PairGroupCommand : IPairGroupCommand
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PairGroupCommand(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SavePairGroupAsync(PairGroup pairGroup, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.PairGroups.Add(pairGroup.ToEntity());
        await context.SaveChangesAsync(token);
    }
}
