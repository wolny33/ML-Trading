using Alpaca.Markets;

namespace TradingBot
{
    public enum OrderType
    {
        Sell,
        Buy
    }
    public struct OrderDetails
    {
        public float limitPrice;
        public float quantity;
        public string symbol;
        public TimeInForce timeInForce;
        public OrderType orderType;
    }
}
