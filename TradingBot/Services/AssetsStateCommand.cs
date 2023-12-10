using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IAssetsStateCommand
{
    Task SaveCurrentAssetsAsync(CancellationToken token = default);
}

public sealed class AssetsStateCommand : IAssetsStateCommand
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly ISystemClock _clock;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public AssetsStateCommand(IAssetsDataSource assetsDataSource, ISystemClock clock,
        IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger)
    {
        _assetsDataSource = assetsDataSource;
        _clock = clock;
        _dbContextFactory = dbContextFactory;
        _logger = logger.ForContext<AssetsStateCommand>();
    }

    public async Task SaveCurrentAssetsAsync(CancellationToken token = default)
    {
        _logger.Debug("Saving current assets information");
        var assets = await _assetsDataSource.GetAssetsAsync(token);

        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.AssetsStates.Add(new AssetsState(assets, _clock.UtcNow).ToEntity());
        await context.SaveChangesAsync(token);

        _logger.Information("Successfully saved current assets information");
    }
}
