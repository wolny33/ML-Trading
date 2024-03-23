using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;

namespace TradingBot.Services.Strategy;

public interface IStrategyParametersService
{
    Task<StrategyParametersConfiguration> GetConfigurationAsync(CancellationToken token = default);

    Task<StrategyParametersConfiguration> SetParametersAsync(StrategyParametersConfiguration newConfig,
        CancellationToken token = default);
}

public sealed class StrategyParametersService : IStrategyParametersService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public StrategyParametersService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<StrategyParametersConfiguration> GetConfigurationAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.StrategyParameters.SingleOrDefaultAsync(token) ??
                     StrategyParametersConfiguration.CreateDefault();

        return StrategyParametersConfiguration.FromEntity(entity);
    }

    public async Task<StrategyParametersConfiguration> SetParametersAsync(StrategyParametersConfiguration newConfig,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.StrategyParameters.SingleOrDefaultAsync(token);
        if (entity is null)
        {
            entity = StrategyParametersConfiguration.CreateDefault();
            context.StrategyParameters.Add(entity);
        }

        newConfig.UpdateEntity(entity);
        await context.SaveChangesAsync(token);

        return newConfig;
    }
}
