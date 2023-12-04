using System.Diagnostics;
using System.Net.Sockets;
using Alpaca.Markets;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Exceptions;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface ITradingActionQuery
{
    Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default);

    Task<TradingAction?> GetTradingActionByIdAsync(Guid id, CancellationToken token = default);

    Task<TradingActionDetails> GetDetailsAsync(Guid id, CancellationToken token = default);

    IEnumerable<TradingAction> CreateMockedTradingActions(DateTimeOffset start, DateTimeOffset end);
}

public sealed class TradingActionQuery : ITradingActionQuery
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingActionQuery(IDbContextFactory<AppDbContext> dbContextFactory, IAlpacaClientFactory clientFactory)
    {
        _dbContextFactory = dbContextFactory;
        _clientFactory = clientFactory;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entities = await context.TradingActions.Where(a =>
            a.CreationTimestamp >= start.ToUnixTimeMilliseconds() &&
            a.CreationTimestamp <= end.ToUnixTimeMilliseconds()).ToListAsync(token);

        using var client = await _clientFactory.CreateTradingClientAsync(token);
        // ReSharper disable once AccessToDisposedClosure
        await Task.WhenAll(entities.Select(e => UpdateActionEntityAsync(e, client, token)));
        await context.SaveChangesAsync(token);

        return entities.Select(TradingAction.FromEntity).ToList();
    }

    public async Task<TradingAction?> GetTradingActionByIdAsync(Guid id,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TradingActions.FirstOrDefaultAsync(a => a.Id == id, token);
        if (entity is null) return null;

        using var client = await _clientFactory.CreateTradingClientAsync(token);
        await UpdateActionEntityAsync(entity, client, token);
        await context.SaveChangesAsync(token);

        return TradingAction.FromEntity(entity);
    }

    public Task<TradingActionDetails> GetDetailsAsync(Guid id, CancellationToken token = default)
    {
        return Task.FromResult(new TradingActionDetails
        {
            Id = id
        });
    }

    public IEnumerable<TradingAction> CreateMockedTradingActions(DateTimeOffset start, DateTimeOffset end)
    {
        var first = start - start.TimeOfDay + TimeSpan.FromDays(1);
        return Enumerable.Range(0, (int)(end - first).TotalDays + 1).Select(i => new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = first + TimeSpan.FromDays(i),
            ExecutedAt = first + TimeSpan.FromDays(i) + TimeSpan.FromMinutes(15),
            Price = (decimal)(Random.Shared.NextDouble() * 99 + 1),
            Quantity = (decimal)Random.Shared.NextDouble(),
            Symbol = new TradingSymbol(Random.Shared.Next() % 2 == 0 ? "AMZN" : "BBBY"),
            OrderType = Random.Shared.Next() % 2 == 0 ? OrderType.LimitBuy : OrderType.LimitSell,
            InForce = TimeInForce.Day,
            Status = (Random.Shared.Next() % 6) switch
            {
                0 => OrderStatus.Accepted,
                1 => OrderStatus.PartiallyFilled,
                2 => OrderStatus.Filled,
                3 => OrderStatus.Canceled,
                4 => OrderStatus.Expired,
                5 => OrderStatus.Rejected,
                _ => throw new UnreachableException()
            },
            AlpacaId = Guid.NewGuid(),
            AverageFillPrice = (decimal)(Random.Shared.NextDouble() * 99 + 1)
        });
    }

    private static async Task UpdateActionEntityAsync(TradingActionEntity entity, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        if (entity.AlpacaId is null || entity.ExecutionTimestamp is not null) return;

        try
        {
            var response = await client.GetOrderAsync(entity.AlpacaId.Value, token);

            var executedAt = response.FilledAtUtc ??
                             response.CancelledAtUtc ?? response.ExpiredAtUtc ?? response.FailedAtUtc;
            entity.Status = response.OrderStatus;
            entity.ExecutionTimestamp = executedAt is not null
                ? new DateTimeOffset(executedAt.Value, TimeSpan.Zero).ToUnixTimeMilliseconds()
                : null;
            entity.AverageFillPrice = (double?)response.AverageFillPrice;
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            throw new AlpacaCallFailedException(e);
        }
    }
}
