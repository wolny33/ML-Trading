using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAssetsStateCommand
{
    Task SaveCurrentAssetsAsync(CancellationToken token = default);
    Task SaveAssetsForBacktestWithIdAsync(Guid id, DateTimeOffset time, CancellationToken token = default);
}

public sealed class AssetsStateCommand : IAssetsStateCommand
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IBacktestAssets _backtestAssets;
    private readonly ISystemClock _clock;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public AssetsStateCommand(IAssetsDataSource assetsDataSource, ISystemClock clock,
        IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger, IBacktestAssets backtestAssets)
    {
        _assetsDataSource = assetsDataSource;
        _clock = clock;
        _dbContextFactory = dbContextFactory;
        _backtestAssets = backtestAssets;
        _logger = logger.ForContext<AssetsStateCommand>();
    }

    public async Task SaveCurrentAssetsAsync(CancellationToken token = default)
    {
        _logger.Debug("Saving current assets information");
        await SaveAssetsStateAsync(
            new AssetsState(await _assetsDataSource.GetCurrentAssetsAsync(token), _clock.UtcNow, null), token);
        _logger.Information("Successfully saved current assets information");
    }

    public Task SaveAssetsForBacktestWithIdAsync(Guid id, DateTimeOffset time, CancellationToken token = default)
    {
        return SaveAssetsStateAsync(new AssetsState(_backtestAssets.GetForBacktestWithId(id), time, id), token);
    }

    private async Task SaveAssetsStateAsync(AssetsState state, CancellationToken token)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.AssetsStates.Add(state.ToEntity());
        await context.SaveChangesAsync(token);
    }
}
