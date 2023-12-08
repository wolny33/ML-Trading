using Microsoft.EntityFrameworkCore;
using TradingBot.Database.Entities;

namespace TradingBot.Database;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<UserCredentialsEntity> Credentials => Set<UserCredentialsEntity>();
    public DbSet<TradingActionEntity> TradingActions => Set<TradingActionEntity>();
    public DbSet<TestModeConfigEntity> TestModeConfiguration => Set<TestModeConfigEntity>();
    public DbSet<InvestmentConfigEntity> InvestmentConfiguration => Set<InvestmentConfigEntity>();
    public DbSet<TradingTaskEntity> TradingTasks => Set<TradingTaskEntity>();
    public DbSet<StrategyParametersEntity> StrategyParameters => Set<StrategyParametersEntity>();
}
