using Microsoft.EntityFrameworkCore;
using TradingBot.Database.Entities;

namespace TradingBot.Database;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<TradingActionEntity> TradingActions => Set<TradingActionEntity>();
    public DbSet<TradingActionDetailsEntity> Details => Set<TradingActionDetailsEntity>();
}
