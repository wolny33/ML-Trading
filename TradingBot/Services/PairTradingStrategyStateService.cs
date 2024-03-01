using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IPairTradingStrategyStateService
{
    Task<PairTradingStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default);
    Task SetCurrentPairGroupIdAsync(Guid pairGroupId, Guid? backtestId, CancellationToken token = default);
}

public class PairTradingStrategyStateService : IPairTradingStrategyStateService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PairTradingStrategyStateService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PairTradingStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity =
            await context.PairTradingStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        if (entity is not null)
        {
            return PairTradingStrategyState.FromEntity(entity);
        }

        entity = new PairTradingStrategyStateEntity { BacktestId = backtestId, CurrentPairGroupId = null };
        context.Add(entity);
        await context.SaveChangesAsync(token);

        return PairTradingStrategyState.FromEntity(entity);
    }

    public async Task SetCurrentPairGroupIdAsync(Guid pairGroupId, Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity =
            await context.PairTradingStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        if (entity is null)
        {
            entity = new PairTradingStrategyStateEntity { BacktestId = backtestId, CurrentPairGroupId = null };
            context.Add(entity);
        }

        entity.CurrentPairGroupId = pairGroupId;

        await context.SaveChangesAsync(token);
    }
}
