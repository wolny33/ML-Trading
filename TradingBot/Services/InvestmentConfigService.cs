using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IInvestmentConfigService
{
    public Task<InvestmentConfiguration> GetConfigurationAsync(CancellationToken token = default);
    public Task<InvestmentConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default);
}

public sealed class InvestmentConfigService : IInvestmentConfigService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public InvestmentConfigService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger.ForContext<InvestmentConfigService>();
    }

    public async Task<InvestmentConfiguration> GetConfigurationAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.InvestmentConfiguration.SingleOrDefaultAsync(token) ??
                     InvestmentConfiguration.CreateDefault();

        return InvestmentConfiguration.FromEntity(entity);
    }

    public async Task<InvestmentConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.InvestmentConfiguration.SingleOrDefaultAsync(token);
        if (entity is null)
        {
            entity = InvestmentConfiguration.CreateDefault();
            context.InvestmentConfiguration.Add(entity);
        }

        entity.Enabled = enabled;
        await context.SaveChangesAsync(token);

        _logger.Information($"Automatic investing was {(enabled ? "enabled" : "disabled")}");
        return InvestmentConfiguration.FromEntity(entity);
    }
}
