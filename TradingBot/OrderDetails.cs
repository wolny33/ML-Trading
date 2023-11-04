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
        public decimal limitPrice;
        public int quantity;
        public string symbol;
        public TimeInForce timeInForce;
        public OrderType orderType;
    }
}
