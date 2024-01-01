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
            .OrderBy(b => b.ExecutionStartTimestamp)
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
        if (!await context.Backtests.AnyAsync(b => b.Id == backtestId, token)) return null;

        var entities = await context.TradingTasks
            .Where(t => t.BacktestId == backtestId)
            .OrderBy(t => t.StartTimestamp)
            .ToListAsync(token);
        return entities.Select(TradingTask.FromEntity).ToList();
    }

    public async Task<IReadOnlyList<AssetsState>?> GetAssetsStatesForBacktestAsync(Guid backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        if (!await context.Backtests.AnyAsync(b => b.Id == backtestId, token)) return null;

        var entities = await context.AssetsStates
            .Include(s => s.HeldPositions)
            .Where(s => s.BacktestId == backtestId)
            .OrderBy(s => s.CreationTimestamp)
            .ToListAsync(token);
        return entities.Select(AssetsState.FromEntity).ToList();
    }
}
