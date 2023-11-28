using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;

namespace TradingBot.Services;

public interface IInvestmentConfigService
{
    public Task<InvestmentConfiguration> GetConfigurationAsync(CancellationToken token = default);
    public Task<InvestmentConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default);
}

public sealed class InvestmentConfigService : IInvestmentConfigService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public InvestmentConfigService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<InvestmentConfiguration> GetConfigurationAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.InvestmentConfiguration.FirstOrDefaultAsync(token) ??
                     InvestmentConfiguration.CreateDefault();

        return InvestmentConfiguration.FromEntity(entity);
    }

    public async Task<InvestmentConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.InvestmentConfiguration.FirstOrDefaultAsync(token);
        if (entity is null)
        {
            entity = InvestmentConfiguration.CreateDefault();
            context.InvestmentConfiguration.Add(entity);
        }

        entity.Enabled = enabled;
        await context.SaveChangesAsync(token);

        return InvestmentConfiguration.FromEntity(entity);
    }
}
