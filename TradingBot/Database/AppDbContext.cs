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
    public DbSet<BuyLosersStrategyStateEntity> BuyLosersStrategyStates => Set<BuyLosersStrategyStateEntity>();
    public DbSet<BuyWinnersStrategyStateEntity> BuyWinnersStrategyStates => Set<BuyWinnersStrategyStateEntity>();
    public DbSet<BuyWinnersEvaluationEntity> BuyWinnersEvaluations => Set<BuyWinnersEvaluationEntity>();
    public DbSet<LoserSymbolToBuyEntity> LoserSymbolsToBuy => Set<LoserSymbolToBuyEntity>();
    public DbSet<WinnerSymbolToBuyEntity> WinnerSymbolsToBuy => Set<WinnerSymbolToBuyEntity>();
}
