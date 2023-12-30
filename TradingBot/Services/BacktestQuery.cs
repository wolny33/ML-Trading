using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IBacktestQuery
{
    Task<IReadOnlyList<Backtest>> GetAllAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default);

    Task<Backtest?> GetByIdAsync(Guid id, CancellationToken token = default);
    Task<IReadOnlyList<TradingTask>?> GetTasksForBacktestAsync(Guid backtestId, CancellationToken token = default);

    Task<IReadOnlyList<AssetsState>?> GetAssetsStatesForBacktestAsync(Guid backtestId,
        CancellationToken token = default);
}

public sealed class BacktestQuery : IBacktestQuery
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public BacktestQuery(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<Backtest>> GetAllAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entities = await context.Backtests.Include(b => b.AssetsStates)
            .Where(b => b.ExecutionStartTimestamp >= start.ToUnixTimeMilliseconds() &&
                        b.ExecutionStartTimestamp <= end.ToUnixTimeMilliseconds())
            .ToListAsync(token);

        return entities.Select(Backtest.FromEntity).ToList();
    }

    public async Task<Backtest?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.Backtests.Include(b => b.AssetsStates).FirstOrDefaultAsync(b => b.Id == id, token);
        return entity is null ? null : Backtest.FromEntity(entity);
    }

    public async Task<IReadOnlyList<TradingTask>?> GetTasksForBacktestAsync(Guid backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.Backtests.Include(b => b.TradingTasks)
            .FirstOrDefaultAsync(b => b.Id == backtestId, token);

        return entity?.TradingTasks.Select(TradingTask.FromEntity).ToList();
    }

    public async Task<IReadOnlyList<AssetsState>?> GetAssetsStatesForBacktestAsync(Guid backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.Backtests.Include(b => b.AssetsStates)
            .FirstOrDefaultAsync(b => b.Id == backtestId, token);

        return entity?.AssetsStates.Select(AssetsState.FromEntity).ToList();
    }
}
