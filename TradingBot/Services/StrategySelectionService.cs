using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;

namespace TradingBot.Services;

public interface IStrategySelectionService
{
    Task<string> GetSelectedNameAsync(CancellationToken token = default);
    Task SetNameAsync(string name, CancellationToken token = default);
}

public sealed class StrategySelectionService : IStrategySelectionService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public StrategySelectionService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public static IReadOnlyList<string> ValidNames =>
        new[] { Strategy.StrategyName, GreedyStrategy.StrategyName, PairTradingStrategy.StrategyName };

    public async Task<string> GetSelectedNameAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.StrategySelection.SingleOrDefaultAsync(token) ??
                     StrategySelectionEntity.MakeDefault();

        return entity.Name;
    }

    public async Task SetNameAsync(string name, CancellationToken token = default)
    {
        if (!IsNameValid(name))
        {
            throw new ArgumentException($"'{name}' is not a valid strategy name", nameof(name));
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.StrategySelection.SingleOrDefaultAsync(token);
        if (entity is null)
        {
            entity = StrategySelectionEntity.MakeDefault();
            context.StrategySelection.Add(entity);
        }

        entity.Name = name;
        await context.SaveChangesAsync(token);
    }

    public static bool IsNameValid(string name)
    {
        return ValidNames.Contains(name);
    }
}
