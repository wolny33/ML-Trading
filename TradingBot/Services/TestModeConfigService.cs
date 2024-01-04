using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface ITestModeConfigService
{
    public Task<TestModeConfiguration> GetConfigurationAsync(CancellationToken token = default);
    public Task<TestModeConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default);
}

public sealed class TestModeConfigService : ITestModeConfigService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public TestModeConfigService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger.ForContext<TestModeConfigService>();
    }

    public async Task<TestModeConfiguration> GetConfigurationAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TestModeConfiguration.SingleOrDefaultAsync(token) ??
                     TestModeConfiguration.CreateDefault();

        return TestModeConfiguration.FromEntity(entity);
    }

    public async Task<TestModeConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TestModeConfiguration.SingleOrDefaultAsync(token);
        if (entity is null)
        {
            entity = TestModeConfiguration.CreateDefault();
            context.TestModeConfiguration.Add(entity);
        }

        entity.Enabled = enabled;
        await context.SaveChangesAsync(token);

        _logger.Information($"Test mode was {(enabled ? "enabled" : "disabled")}");
        return TestModeConfiguration.FromEntity(entity);
    }
}
