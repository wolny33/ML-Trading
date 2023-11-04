using Alpaca.Markets;
using Microsoft.AspNetCore.Routing.Constraints;
using TradingBot.Entities;

namespace TradingBot
{
    public class Bot
    {
        IAlpacaTradingClient _client;
        public Bot(IAlpacaTradingClient client)
        {
            _client = client;
        }
        private float PredictPrice(string symbol)
        {
            throw new NotImplementedException();
        }
        private List<OrderDetails> GetNextMoveFromStrategy()
        {
            throw new NotImplementedException();
        }
        private void MakeNextMoveFromStrategy()
        { 
            throw new NotImplementedException(); 
        }
        private async Task<bool> PlaceOrder(OrderDetails orderDetails)
        {
            if (orderDetails.orderType == OrderType.Sell)
            {
                var order = await _client.PostOrderAsync(
                    LimitOrder.Sell(orderDetails.symbol, OrderQuantity.FromInt64(orderDetails.quantity), orderDetails.limitPrice).WithDuration(orderDetails.timeInForce));
            }
            else
            {
                var order = await _client.PostOrderAsync(
                    LimitOrder.Buy(orderDetails.symbol, OrderQuantity.FromInt64(orderDetails.quantity), orderDetails.limitPrice).WithDuration(orderDetails.timeInForce));
            }
            throw new NotImplementedException();
        }
    }
}
