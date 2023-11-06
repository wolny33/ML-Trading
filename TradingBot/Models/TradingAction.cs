using Alpaca.Markets;

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
}
