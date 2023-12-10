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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Hashed value is hardcoded, because PasswordHasher does not generate deterministic hashes 
        modelBuilder.Entity<UserCredentialsEntity>().HasData(new UserCredentialsEntity
        {
            Id = Guid.Empty,
            Username = "admin",
            HashedPassword = "AQAAAAIAAYagAAAAEKYyNm9AKgWuGR19nYSNT/7HYWJDCeC63fZKh/MfFaIaNIMhTKXzHLRXjEQ2uX6Qog=="
        });
    }
}
