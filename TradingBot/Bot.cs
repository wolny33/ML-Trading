using Alpaca.Markets;
using Microsoft.AspNetCore.Routing.Constraints;

namespace TradingBot
{
    public class Bot
    {
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
        private bool PlaceOrder(OrderDetails orderDetails)
        {
            throw new NotImplementedException();
        }
    }
}
