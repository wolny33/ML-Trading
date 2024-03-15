using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;

namespace TradingBot.Services.Strategy;

public interface IStrategySelectionService
{
    Task<string> GetSelectedNameAsync(Guid? backtestId, CancellationToken token = default);
    Task SetNameAsync(string name, CancellationToken token = default);
}

public sealed class StrategySelectionService : IStrategySelectionService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConcurrentDictionary<Guid, string> _strategySelections = new();

    public StrategySelectionService(IDbContextFactory<AppDbContext> dbContextFactory, IServiceScopeFactory scopeFactory)
    {
        _dbContextFactory = dbContextFactory;
        _scopeFactory = scopeFactory;
    }

    public static IReadOnlyList<string> ValidNames =>
        new[]
        {
            Strategy.StrategyName, GreedyStrategy.StrategyName, BuyLosersStrategy.StrategyName,
            BuyWinnersStrategy.StrategyName, PcaStrategy.StrategyName
        };

    public async Task<string> GetSelectedNameAsync(Guid? backtestId, CancellationToken token = default)
    {
        if (backtestId is not null && _strategySelections.TryGetValue(backtestId.Value, out var selection))
        {
            return selection;
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.StrategySelection.SingleOrDefaultAsync(token) ??
                     StrategySelectionEntity.MakeDefault();

        if (backtestId is not null)
        {
            _strategySelections[backtestId.Value] = entity.Name;
        }

        return entity.Name;
    }

    public async Task SetNameAsync(string name, CancellationToken token = default)
    {
        if (!IsNameValid(name))
        {
            throw new ArgumentException($"'{name}' is not a valid strategy name", nameof(name));
        }

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var strategy = await scope.ServiceProvider.GetRequiredService<IStrategyFactory>().CreateAsync(token);
            await strategy.HandleDeselectionAsync(token);
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
