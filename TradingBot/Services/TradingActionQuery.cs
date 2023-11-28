using Alpaca.Markets;
using Microsoft.EntityFrameworkCore;
using TradingBot.Database;
using TradingBot.Models;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface ITradingActionQuery
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default);

    public Task<TradingActionDetails> GetDetailsAsync(Guid id, CancellationToken token = default);
}

public sealed class TradingActionQuery : ITradingActionQuery
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public TradingActionQuery(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(DateTimeOffset start, DateTimeOffset end,
        CancellationToken token = default)
    {
        return Task.FromResult<IReadOnlyList<TradingAction>>(CreateTradingActions(start, end).ToList());
    }

    public Task<TradingActionDetails> GetDetailsAsync(Guid id, CancellationToken token = default)
    {
        return Task.FromResult(new TradingActionDetails
        {
            Id = id
        });
    }

    private static IEnumerable<TradingAction> CreateTradingActions(DateTimeOffset start, DateTimeOffset end)
    {
        var first = start - start.TimeOfDay + TimeSpan.FromDays(1);
        return Enumerable.Range(0, (int)(end - first).TotalDays + 1).Select(i => new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = first + TimeSpan.FromDays(i),
            Price = (decimal)(Random.Shared.NextDouble() * 99 + 1),
            Quantity = (decimal)Random.Shared.NextDouble(),
            Symbol = new TradingSymbol(Random.Shared.Next() % 2 == 0 ? "AMZN" : "BBBY"),
            OrderType = Random.Shared.Next() % 2 == 0 ? OrderType.LimitBuy : OrderType.LimitSell,
            InForce = TimeInForce.Day
        });
    }
}
