using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface ITradingTaskQuery
{
    Task<IReadOnlyList<TradingTask>> GetTradingTasksAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default);

    Task<TradingTask?> GetTradingTaskByIdAsync(Guid id, CancellationToken token = default);
}

public sealed class TradingTaskQuery : ITradingTaskQuery
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingTaskQuery(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<TradingTask>> GetTradingTasksAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entities = await context.TradingTasks.Where(t =>
                t.StartTimestamp >= start.ToUnixTimeMilliseconds() && t.StartTimestamp <= end.ToUnixTimeMilliseconds())
            .ToListAsync(token);

        return entities.Select(TradingTask.FromEntity).ToList();
    }

    public async Task<TradingTask?> GetTradingTaskByIdAsync(Guid id, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TradingTasks.FirstOrDefaultAsync(t => t.Id == id, token);
        return entity is null ? null : TradingTask.FromEntity(entity);
    }
}
