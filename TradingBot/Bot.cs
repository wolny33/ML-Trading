using Alpaca.Markets;

namespace TradingBot;

public sealed class Bot
{
    private readonly IAlpacaTradingClient _client;

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
        if (orderDetails.OrderType == OrderType.Sell)
        {
            var order = await _client.PostOrderAsync(
                LimitOrder.Sell(orderDetails.Symbol, OrderQuantity.FromInt64(orderDetails.Quantity),
                    orderDetails.LimitPrice).WithDuration(orderDetails.InForce));
        }
        else
        {
            var order = await _client.PostOrderAsync(
                LimitOrder.Buy(orderDetails.Symbol, OrderQuantity.FromInt64(orderDetails.Quantity),
                    orderDetails.LimitPrice).WithDuration(orderDetails.InForce));
        }

        throw new NotImplementedException();
    }
}
