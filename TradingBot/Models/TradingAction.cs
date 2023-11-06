using Alpaca.Markets;
using TradingBot.Database.Entities;

namespace TradingBot.Models;

public enum OrderType
{
    LimitSell,
    LimitBuy,
    MarketSell,
    MarketBuy
}

public sealed record TradingSymbol(string Value);

public sealed class TradingAction
{
    public required Guid Id { get; init; }
    public required decimal? Price { get; init; }
    public required decimal Quantity { get; init; }
    public required TradingSymbol Symbol { get; init; }
    public required TimeInForce InForce { get; init; }
    public required OrderType OrderType { get; init; }

    public static TradingAction FromEntity(TradingActionEntity entity)
    {
        return new TradingAction
        {
            Id = entity.Id,
            Price = entity.Price is not null ? (decimal)entity.Price : null,
            Quantity = (decimal)entity.Quantity,
            Symbol = new TradingSymbol(entity.Symbol),
            InForce = entity.InForce,
            OrderType = entity.OrderType
        };
    }

    public TradingActionEntity ToEntity()
    {
        return new TradingActionEntity
        {
            Id = Id,
            Price = Price is not null ? (double)Price : null,
            Quantity = (double)Quantity,
            Symbol = Symbol.Value,
            InForce = InForce,
            OrderType = OrderType,
            Details = new TradingActionDetailsEntity
            {
                TradingActionId = Id
            }
        };
    }
}
