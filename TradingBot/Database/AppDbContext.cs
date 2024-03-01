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
    public DbSet<StrategySelectionEntity> StrategySelection => Set<StrategySelectionEntity>();
    public DbSet<AssetsStateEntity> AssetsStates => Set<AssetsStateEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<BacktestEntity> Backtests => Set<BacktestEntity>();
    public DbSet<PairGroupEntity> PairGroups => Set<PairGroupEntity>();
    public DbSet<PairEntity> Pairs => Set<PairEntity>();
    public DbSet<PairTradingStrategyStateEntity> PairTradingStrategyStates => Set<PairTradingStrategyStateEntity>();
}
