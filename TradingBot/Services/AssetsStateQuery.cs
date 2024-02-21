﻿using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IAssetsStateQuery
{
    Task<AssetsState?> GetEarliestStateAsync(Mode mode, CancellationToken token = default);

    Task<AssetsState?> GetLatestStateAsync(Mode mode, CancellationToken token = default);

    Task<IReadOnlyList<AssetsState>> GetStatesFromRangeAsync(DateTimeOffset start, DateTimeOffset end, Mode mode,
        CancellationToken token = default);
}

public sealed class AssetsStateQuery : IAssetsStateQuery
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AssetsStateQuery(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<AssetsState?> GetEarliestStateAsync(Mode mode, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.AssetsStates.Include(s => s.HeldPositions)
            .Where(s => s.Mode == mode)
            .OrderBy(s => s.CreationTimestamp)
            .FirstOrDefaultAsync(token);
        return entity is null ? null : AssetsState.FromEntity(entity);
    }

    public async Task<AssetsState?> GetLatestStateAsync(Mode mode, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.AssetsStates.Include(s => s.HeldPositions)
            .Where(s => s.Mode == mode)
            .OrderByDescending(s => s.CreationTimestamp)
            .FirstOrDefaultAsync(token);
        return entity is null ? null : AssetsState.FromEntity(entity);
    }

    public async Task<IReadOnlyList<AssetsState>> GetStatesFromRangeAsync(DateTimeOffset start, DateTimeOffset end,
        Mode mode, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entities = await context.AssetsStates.Include(s => s.HeldPositions)
            .Where(s => s.Mode == mode)
            .OrderByDescending(s => s.CreationTimestamp)
            .Where(s =>
                s.CreationTimestamp >= start.ToUnixTimeMilliseconds() &&
                s.CreationTimestamp <= end.ToUnixTimeMilliseconds())
            .ToListAsync(token);
        return entities.Select(AssetsState.FromEntity).ToList();
    }
}
