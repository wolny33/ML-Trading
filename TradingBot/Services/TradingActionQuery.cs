using Alpaca.Markets;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface ITradingActionQuery
{
    Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default);

    Task<TradingAction?> GetTradingActionByIdAsync(Guid id, CancellationToken token = default);

    Task<IReadOnlyList<TradingAction>?> GetActionsForTaskWithIdAsync(Guid taskId, CancellationToken token = default);
}

public sealed class TradingActionQuery : ITradingActionQuery
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public TradingActionQuery(IDbContextFactory<AppDbContext> dbContextFactory, IAlpacaClientFactory clientFactory,
        ILogger logger)
    {
        _dbContextFactory = dbContextFactory;
        _clientFactory = clientFactory;
        _logger = logger.ForContext<TradingActionQuery>();
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entities = await context.TradingActions.Include(a => a.TradingTask)
            .Where(a => a.TradingTask == null || a.TradingTask.BacktestId == null).Where(a =>
                a.CreationTimestamp >= start.ToUnixTimeMilliseconds() &&
                a.CreationTimestamp <= end.ToUnixTimeMilliseconds()).ToListAsync(token);

        using var client = await _clientFactory.CreateTradingClientAsync(token);
        // ReSharper disable once AccessToDisposedClosure
        await Task.WhenAll(entities.Select(e => UpdateActionEntityAsync(e, client, token)));
        await context.SaveChangesAsync(token);

        return entities.Select(TradingAction.FromEntity).ToList();
    }

    public async Task<TradingAction?> GetTradingActionByIdAsync(Guid id, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TradingActions.FirstOrDefaultAsync(a => a.Id == id, token);
        if (entity is null) return null;

        using var client = await _clientFactory.CreateTradingClientAsync(token);
        await UpdateActionEntityAsync(entity, client, token);
        await context.SaveChangesAsync(token);

        return TradingAction.FromEntity(entity);
    }

    public async Task<IReadOnlyList<TradingAction>?> GetActionsForTaskWithIdAsync(Guid taskId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entities =
            (await context.TradingTasks.Include(t => t.TradingActions).FirstOrDefaultAsync(t => t.Id == taskId, token))
            ?.TradingActions;

        if (entities is null) return null;

        using var client = await _clientFactory.CreateTradingClientAsync(token);
        // ReSharper disable once AccessToDisposedClosure
        await Task.WhenAll(entities.Select(e => UpdateActionEntityAsync(e, client, token)));
        await context.SaveChangesAsync(token);

        return entities.Select(TradingAction.FromEntity).ToList();
    }

    private async Task UpdateActionEntityAsync(TradingActionEntity entity, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        if (entity.AlpacaId is null || entity.ExecutionTimestamp is not null) return;

        _logger.Verbose("Updating action {Id}: {Action}", entity.Id, entity);

        var response = await client.GetOrderAsync(entity.AlpacaId.Value, token).ReturnNullOnRequestLimit(_logger)
            .ExecuteWithErrorHandling(_logger);
        if (response is null)
        {
            _logger.Debug("Action {Id} was not updated because Alpaca request limit was hit", entity.Id);
            return;
        }

        var executedAt = response.FilledAtUtc
                         ?? response.CancelledAtUtc
                         ?? response.ExpiredAtUtc
                         ?? response.FailedAtUtc;

        _logger.Verbose(
            "Retrieved properties: Execution time = {ExecutionTime}, Status = {Status}, Fill price = {FillPrice}",
            executedAt, response.OrderStatus.ToString(), response.AverageFillPrice);

        entity.Status = response.OrderStatus;
        entity.ExecutionTimestamp = executedAt is not null
            ? new DateTimeOffset(executedAt.Value, TimeSpan.Zero).ToUnixTimeMilliseconds()
            : null;
        entity.AverageFillPrice = (double?)response.AverageFillPrice;
    }
}
