using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;

namespace TradingBot.Services.Strategy;

public interface IBuyWinnersStrategyStateService
{
    Task<BuyWinnersStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default);
    Task SetNextExecutionDayAsync(DateOnly day, Guid? backtestId, CancellationToken token = default);
    Task ClearNextExecutionDayAsync(CancellationToken token = default);
    Task SaveNewEvaluationAsync(BuyWinnersEvaluation evaluation, Guid? backtestId, CancellationToken token = default);

    Task MarkEvaluationAsBoughtAsync(IReadOnlyList<Guid> actionIds, Guid evaluationId,
        CancellationToken token = default);

    Task DeleteEvaluationAsync(Guid evaluationId, CancellationToken token = default);
}

public sealed class BuyWinnersStrategyStateService : IBuyWinnersStrategyStateService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public BuyWinnersStrategyStateService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<BuyWinnersStrategyState> GetStateAsync(Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyWinnersStrategyStates
            .Include(s => s.Evaluations)
            .ThenInclude(e => e.SymbolsToBuy)
            .FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);

        return BuyWinnersStrategyState.FromEntity(EnsureEntityExists(entity, backtestId, context));
    }

    public async Task SetNextExecutionDayAsync(DateOnly day, Guid? backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyWinnersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        entity = EnsureEntityExists(entity, backtestId, context);

        entity.NextEvaluationDay = day;

        await context.SaveChangesAsync(token);
    }

    public async Task ClearNextExecutionDayAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyWinnersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == null, token);
        if (entity is null) return;

        entity.NextEvaluationDay = null;

        await context.SaveChangesAsync(token);
    }

    public async Task SaveNewEvaluationAsync(BuyWinnersEvaluation evaluation, Guid? backtestId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.BuyWinnersEvaluations.Add(evaluation.ToEntity(backtestId));
        await context.SaveChangesAsync(token);
    }

    public async Task MarkEvaluationAsBoughtAsync(IReadOnlyList<Guid> actionIds, Guid evaluationId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.BuyWinnersEvaluations
            .Where(e => e.Id == evaluationId)
            .ExecuteUpdateAsync(evaluation =>
                    evaluation.SetProperty(e => e.Bought, true),
                token);

        context.WinnerBuyActions.AddRange(actionIds.Select(actionId => new BuyWinnersBuyActionEntity
        {
            Id = Guid.NewGuid(),
            EvaluationId = evaluationId,
            ActionId = actionId
        }));

        await context.SaveChangesAsync(token);
    }

    public async Task DeleteEvaluationAsync(Guid evaluationId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.BuyWinnersEvaluations.Where(e => e.Id == evaluationId).ExecuteDeleteAsync(token);
    }

    private static BuyWinnersStrategyStateEntity EnsureEntityExists(BuyWinnersStrategyStateEntity? entity,
        Guid? backtestId, AppDbContext context)
    {
        if (entity is not null) return entity;
        var newEntity = new BuyWinnersStrategyStateEntity { BacktestId = backtestId, NextEvaluationDay = null };
        context.BuyWinnersStrategyStates.Add(newEntity);
        return newEntity;
    }
}
