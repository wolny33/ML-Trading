using Alpaca.Markets;

namespace TradingBot;

public enum OrderType
{
    Sell,
    Buy
}

public sealed class OrderDetails
{
    public required decimal LimitPrice { get; init; }
    public required int Quantity { get; init; }
    public required string Symbol { get; init; }
    public required TimeInForce InForce { get; init; }
    public required OrderType OrderType { get; init; }
}
