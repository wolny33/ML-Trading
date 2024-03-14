using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public interface IBuyLosersStrategyStateService
{
    Task<BuyLosersStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default);

    Task SetSymbolsToBuyAsync(IReadOnlyList<TradingSymbol> symbols, Guid? backtestId,
        CancellationToken token = default);

    Task ClearSymbolsToBuyAsync(Guid? backtestId, CancellationToken token = default);

    Task SetNextExecutionDayAsync(DateOnly day, Guid? backtestId, CancellationToken token = default);
    Task ClearNextExecutionDayAsync(CancellationToken token = default);
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
        backtestId ??= BuyLosersStrategyState.NormalExecutionStateId;
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyLosersStrategyStates
            .Include(s => s.SymbolsToBuy)
            .FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);

        return entity is not null
            ? BuyLosersStrategyState.FromEntity(entity)
            : new BuyLosersStrategyState { NextEvaluationDay = null, SymbolsToBuy = Array.Empty<TradingSymbol>() };
    }

    public async Task SetSymbolsToBuyAsync(IReadOnlyList<TradingSymbol> symbols, Guid? backtestId,
        CancellationToken token = default)
    {
        backtestId ??= BuyLosersStrategyState.NormalExecutionStateId;
        await EnsureEntityExistsAsync(backtestId.Value, token);

        await using var context = await _dbContextFactory.CreateDbContextAsync(token);

        foreach (var symbol in symbols)
        {
            context.LoserSymbolsToBuy.Add(new LoserSymbolToBuyEntity
            {
                Id = Guid.NewGuid(),
                StrategyStateBacktestId = backtestId.Value,
                Symbol = symbol.Value
            });
        }

        await context.SaveChangesAsync(token);
    }

    public async Task ClearSymbolsToBuyAsync(Guid? backtestId, CancellationToken token = default)
    {
        backtestId ??= BuyLosersStrategyState.NormalExecutionStateId;
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.LoserSymbolsToBuy.Where(s => s.StrategyStateBacktestId == backtestId).ExecuteDeleteAsync(token);
    }

    public async Task SetNextExecutionDayAsync(DateOnly day, Guid? backtestId, CancellationToken token = default)
    {
        backtestId ??= BuyLosersStrategyState.NormalExecutionStateId;
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyLosersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        if (entity is null)
        {
            entity = new BuyLosersStrategyStateEntity { BacktestId = backtestId.Value, NextEvaluationDay = null };
            context.BuyLosersStrategyStates.Add(entity);
        }

        entity.NextEvaluationDay = day;

        await context.SaveChangesAsync(token);
    }

    public async Task ClearNextExecutionDayAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity =
            await context.BuyLosersStrategyStates.FirstOrDefaultAsync(
                s => s.BacktestId == BuyLosersStrategyState.NormalExecutionStateId, token);
        if (entity is null)
        {
            return;
        }

        entity.NextEvaluationDay = null;

        await context.SaveChangesAsync(token);
    }

    private async Task EnsureEntityExistsAsync(Guid backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyLosersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        if (entity is not null)
        {
            return;
        }

        var newEntity = new BuyLosersStrategyStateEntity { BacktestId = backtestId, NextEvaluationDay = null };
        context.BuyLosersStrategyStates.Add(newEntity);
        await context.SaveChangesAsync(token);
    }
}
