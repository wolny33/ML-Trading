using Microsoft.EntityFrameworkCore;
using TradingBot.Configuration;
using TradingBot.Database;

namespace TradingBot.Services;

public interface ITestModeConfigService
{
    public Task<TestModeConfiguration> GetConfigurationAsync(CancellationToken token = default);
    public Task<TestModeConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default);
}

public sealed class TestModeConfigService : ITestModeConfigService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TestModeConfigService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TestModeConfiguration> GetConfigurationAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TestModeConfiguration.FirstOrDefaultAsync(token) ??
                     TestModeConfiguration.CreateDefault();

        return TestModeConfiguration.FromEntity(entity);
    }

    public async Task<TestModeConfiguration> SetEnabledAsync(bool enabled, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TestModeConfiguration.FirstOrDefaultAsync(token);
        if (entity is null)
        {
            entity = TestModeConfiguration.CreateDefault();
            context.TestModeConfiguration.Add(entity);
        }

        entity.Enabled = enabled;
        await context.SaveChangesAsync(token);

        return TestModeConfiguration.FromEntity(entity);
    }
}
