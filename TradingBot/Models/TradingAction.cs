using Alpaca.Markets;
using TradingBot.Database.Entities;
using TradingBot.Dto;

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
    public required DateTimeOffset CreatedAt { get; init; }
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
            CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(entity.CreationTimestamp),
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
            CreationTimestamp = CreatedAt.ToUnixTimeMilliseconds(),
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

    public TradingActionResponse ToResponse()
    {
        return new TradingActionResponse
        {
            Id = Id,
            CreatedAt = CreatedAt,
            Price = Price,
            Quantity = Quantity,
            Symbol = Symbol.Value,
            InForce = InForce.ToString(),
            OrderType = OrderType.ToString()
        };
    }
}
