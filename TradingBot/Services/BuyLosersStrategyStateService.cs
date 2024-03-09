using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IBuyLosersStrategyStateService
{
    Task<BuyLosersStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default);

    Task SetSymbolsToBuyAsync(IReadOnlyList<TradingSymbol> symbols, Guid? backtestId,
        CancellationToken token = default);

    Task ClearSymbolsToBuyAsync(Guid? backtestId, CancellationToken token = default);

    Task SetNextExecutionDay(DateOnly day, Guid? backtestId, CancellationToken token = default);
}

public class BuyLosersStrategyStateService : IBuyLosersStrategyStateService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public BuyLosersStrategyStateService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<BuyLosersStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity =
            await context.BuyLosersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);

        return BuyLosersStrategyState.FromEntity(EnsureEntityExists(entity, backtestId, context));
    }

    public async Task SetSymbolsToBuyAsync(IReadOnlyList<TradingSymbol> symbols, Guid? backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);

        foreach (var symbol in symbols)
            context.SymbolsToBuy.Add(new SymbolToBuyEntity
            {
                Id = Guid.NewGuid(),
                StrategyStateBacktestId = backtestId,
                Symbol = symbol.Value
            });

        await context.SaveChangesAsync(token);
    }

    public async Task ClearSymbolsToBuyAsync(Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.SymbolsToBuy.Where(s => s.StrategyStateBacktestId == backtestId).ExecuteDeleteAsync(token);
    }

    public async Task SetNextExecutionDay(DateOnly day, Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity =
            await context.BuyLosersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        entity = EnsureEntityExists(entity, backtestId, context);

        entity.NextEvaluationDay = day;

        await context.SaveChangesAsync(token);
    }

    private static BuyLosersStrategyStateEntity EnsureEntityExists(BuyLosersStrategyStateEntity? entity,
        Guid? backtestId, AppDbContext context)
    {
        if (entity is not null) return entity;
        var newEntity = new BuyLosersStrategyStateEntity { BacktestId = backtestId, NextEvaluationDay = null };
        context.BuyLosersStrategyStates.Add(newEntity);
        return newEntity;
    }
}