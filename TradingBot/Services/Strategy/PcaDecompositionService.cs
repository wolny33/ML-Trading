using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public interface IPcaDecompositionService
{
    Task<PcaDecomposition?> GetLatestDecompositionAsync(Guid? backtestId, CancellationToken token = default);
    Task SaveDecompositionAsync(PcaDecomposition decomposition, Guid? backtestId, CancellationToken token = default);
}

public sealed class PcaDecompositionService : IPcaDecompositionService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PcaDecompositionService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PcaDecomposition?> GetLatestDecompositionAsync(Guid? backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.PcaDecompositions
            .OrderByDescending(e => e.CreationTimestamp)
            .FirstOrDefaultAsync(e => e.BacktestId == (backtestId ?? PcaDecomposition.NormalExecutionId), token);
        return entity is null ? null : PcaDecomposition.FromEntity(entity);
    }

    public async Task SaveDecompositionAsync(PcaDecomposition decomposition, Guid? backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.PcaDecompositions.Add(decomposition.ToEntity(backtestId));
        await context.SaveChangesAsync(token);
    }
}
