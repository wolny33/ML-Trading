using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;

namespace TradingBot.Services;

public interface IStrategyParametersService
{
    public Task<StrategyParametersConfiguration> GetConfigurationAsync(CancellationToken token = default);

    public Task<StrategyParametersConfiguration> SetParametersAsync(int maxStocksBuyCount, int minDaysDecreasing,
        int minDaysIncreasing, double topGrowingSymbolsBuyRatio, CancellationToken token = default);
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

    public async Task<StrategyParametersConfiguration> SetParametersAsync(int maxStocksBuyCount, int minDaysDecreasing,
        int minDaysIncreasing, double topGrowingSymbolsBuyRatio, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.StrategyParameters.SingleOrDefaultAsync(token);
        if (entity is null)
        {
            entity = StrategyParametersConfiguration.CreateDefault();
            context.StrategyParameters.Add(entity);
        }

        entity.MaxStocksBuyCount = maxStocksBuyCount;
        entity.MinDaysDecreasing = minDaysDecreasing;
        entity.MinDaysIncreasing = minDaysIncreasing;
        entity.TopGrowingSymbolsBuyRatio = topGrowingSymbolsBuyRatio;
        await context.SaveChangesAsync(token);

        return StrategyParametersConfiguration.FromEntity(entity);
    }
}
