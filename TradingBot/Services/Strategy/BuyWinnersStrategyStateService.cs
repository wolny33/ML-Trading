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

    Task SaveActionIdsForEvaluationAsync(IReadOnlyList<Guid> actionIds, Guid evaluationId,
        CancellationToken token = default);

    Task MarkEvaluationAsBoughtAsync(Guid evaluationId, CancellationToken token = default);
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
        backtestId ??= BuyWinnersStrategyState.NormalExecutionStateId;
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyWinnersStrategyStates
            .Include(s => s.Evaluations)
            .ThenInclude(e => e.SymbolsToBuy)
            .Include(e => e.Evaluations)
            .ThenInclude(e => e.Actions)
            .FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);

        return entity is not null
            ? BuyWinnersStrategyState.FromEntity(entity)
            : new BuyWinnersStrategyState
            {
                NextEvaluationDay = null,
                Evaluations = Array.Empty<BuyWinnersEvaluation>()
            };
    }

    public async Task SetNextExecutionDayAsync(DateOnly day, Guid? backtestId, CancellationToken token = default)
    {
        backtestId ??= BuyWinnersStrategyState.NormalExecutionStateId;
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyWinnersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        if (entity is null)
        {
            entity = new BuyWinnersStrategyStateEntity { BacktestId = backtestId.Value, NextEvaluationDay = null };
            context.BuyWinnersStrategyStates.Add(entity);
        }

        entity.NextEvaluationDay = day;

        await context.SaveChangesAsync(token);
    }

    public async Task ClearNextExecutionDayAsync(CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity =
            await context.BuyWinnersStrategyStates.FirstOrDefaultAsync(
                s => s.BacktestId == BuyWinnersStrategyState.NormalExecutionStateId, token);
        if (entity is null)
        {
            return;
        }

        entity.NextEvaluationDay = null;

        await context.SaveChangesAsync(token);
    }

    public async Task SaveNewEvaluationAsync(BuyWinnersEvaluation evaluation, Guid? backtestId,
        CancellationToken token = default)
    {
        backtestId ??= BuyWinnersStrategyState.NormalExecutionStateId;
        await EnsureEntityExistsAsync(backtestId.Value, token);

        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.BuyWinnersEvaluations.Add(evaluation.ToEntity(backtestId.Value));
        await context.SaveChangesAsync(token);
    }

    public async Task SaveActionIdsForEvaluationAsync(IReadOnlyList<Guid> actionIds, Guid evaluationId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        context.WinnerBuyActions.AddRange(actionIds.Select(actionId => new BuyWinnersBuyActionEntity
        {
            Id = Guid.NewGuid(),
            EvaluationId = evaluationId,
            ActionId = actionId
        }));

        await context.SaveChangesAsync(token);
    }

    public async Task MarkEvaluationAsBoughtAsync(Guid evaluationId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.BuyWinnersEvaluations
            .Where(e => e.Id == evaluationId)
            .ExecuteUpdateAsync(evaluation =>
                    evaluation.SetProperty(e => e.Bought, true),
                token);
    }

    public async Task DeleteEvaluationAsync(Guid evaluationId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        await context.BuyWinnersEvaluations.Where(e => e.Id == evaluationId).ExecuteDeleteAsync(token);
    }

    private async Task EnsureEntityExistsAsync(Guid backtestId, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.BuyWinnersStrategyStates.FirstOrDefaultAsync(s => s.BacktestId == backtestId, token);
        if (entity is not null)
        {
            return;
        }

        var newEntity = new BuyWinnersStrategyStateEntity { BacktestId = backtestId, NextEvaluationDay = null };
        context.BuyWinnersStrategyStates.Add(newEntity);
        await context.SaveChangesAsync(token);
    }
}
