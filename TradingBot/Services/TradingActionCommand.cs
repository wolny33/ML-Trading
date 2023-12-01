using System.Net.Sockets;
using Alpaca.Markets;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Database.Entities;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;

namespace TradingBot.Services;

public interface ITradingActionCommand
{
    Task SaveActionWithAlpacaIdAsync(TradingAction action, Guid alpacaId, CancellationToken token = default);
    Task UpdateActionAsync(Guid id, CancellationToken token = default);
}

public sealed class TradingActionCommand : ITradingActionCommand
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingActionCommand(IDbContextFactory<AppDbContext> dbContextFactory, IAlpacaClientFactory clientFactory)
    {
        _dbContextFactory = dbContextFactory;
        _clientFactory = clientFactory;
    }

    public async Task SaveActionWithAlpacaIdAsync(TradingAction action, Guid alpacaId,
        CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);

        var entity = action.ToEntity();
        entity.AlpacaId = alpacaId;

        context.TradingActions.Add(entity);
        await context.SaveChangesAsync(token);
    }

    public async Task UpdateActionAsync(Guid id, CancellationToken token = default)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.TradingActions.FirstOrDefaultAsync(a => a.Id == id, token);
        if (entity is null) return;

        using var client = await _clientFactory.CreateTradingClientAsync(token);
        await UpdateActionEntityAsync(entity, client, token);
        await context.SaveChangesAsync(token);
    }

    private async Task UpdateActionEntityAsync(TradingActionEntity entity, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        if (entity.AlpacaId is null) return;

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
        catch (RestClientErrorException e)
        {
            throw new UnsuccessfulAlpacaResponseException(e.HttpStatusCode is not null ? (int)e.HttpStatusCode : 0,
                e.Message);
        }
        catch (Exception e) when (e is HttpRequestException or SocketException or TaskCanceledException)
        {
            throw new AlpacaCallFailedException(e);
        }
    }
}
