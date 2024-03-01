using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IPairGroupQuery
{
    Task<PairGroup?> GetByIdAsync(Guid id, CancellationToken token = default);
}

public sealed class PairGroupQuery : IPairGroupQuery
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PairGroupQuery(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PairGroup?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.PairGroups.Include(g => g.Pairs).FirstOrDefaultAsync(g => g.Id == id, token);

        return entity is null ? null : PairGroup.FromEntity(entity);
    }
}
