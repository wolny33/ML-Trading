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
    public OrderStatus? Status { get; set; }
    public DateTimeOffset? ExecutedAt { get; init; }
    public Guid? AlpacaId { get; init; }
    public decimal? AverageFillPrice { get; init; }

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
            OrderType = entity.OrderType,
            Status = entity.Status,
            ExecutedAt = entity.ExecutionTimestamp is not null
                ? DateTimeOffset.FromUnixTimeMilliseconds(entity.ExecutionTimestamp.Value)
                : null,
            AlpacaId = entity.AlpacaId,
            AverageFillPrice = (decimal?)entity.AverageFillPrice
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
            Status = Status,
            ExecutionTimestamp = ExecutedAt?.ToUnixTimeMilliseconds(),
            AlpacaId = AlpacaId,
            AverageFillPrice = (double?)AverageFillPrice,
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
            OrderType = OrderType.ToString(),
            Status = Status?.ToString() ?? "NotPosted",
            ExecutedAt = ExecutedAt,
            AlpacaId = AlpacaId,
            AverageFillPrice = AverageFillPrice
        };
    }

    public static TradingAction MarketBuy(TradingSymbol symbol, decimal quantity, DateTimeOffset createdAt)
    {
        return new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            Quantity = quantity,
            Price = null,
            Symbol = symbol,
            InForce = TimeInForce.Gtc,
            OrderType = OrderType.MarketBuy
        };
    }

    public static TradingAction MarketSell(TradingSymbol symbol, decimal quantity, DateTimeOffset createdAt)
    {
        return new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            Quantity = quantity,
            Price = null,
            Symbol = symbol,
            InForce = TimeInForce.Gtc,
            OrderType = OrderType.MarketSell
        };
    }

    public static TradingAction LimitBuy(TradingSymbol symbol, decimal quantity, decimal price,
        DateTimeOffset createdAt)
    {
        return new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            Quantity = quantity,
            Price = price,
            Symbol = symbol,
            InForce = TimeInForce.Gtc,
            OrderType = OrderType.LimitBuy
        };
    }

    public static TradingAction LimitSell(TradingSymbol symbol, decimal quantity, decimal price,
        DateTimeOffset createdAt)
    {
        return new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = createdAt,
            Quantity = quantity,
            Price = price,
            Symbol = symbol,
            InForce = TimeInForce.Gtc,
            OrderType = OrderType.LimitSell
        };
    }
}
