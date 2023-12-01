using System.Net.Sockets;
using Alpaca.Markets;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface IActionExecutor
{
    public Task ExecuteTradingActionsAsync(CancellationToken token = default);
}

public sealed class ActionExecutor : IActionExecutor
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ITradingActionCommand _command;
    private readonly IStrategy _strategy;

    public ActionExecutor(IStrategy strategy, IAlpacaClientFactory clientFactory, ITradingActionCommand command)
    {
        _strategy = strategy;
        _clientFactory = clientFactory;
        _command = command;
    }

    public async Task ExecuteTradingActionsAsync(CancellationToken token = default)
    {
        var actions = await _strategy.GetTradingActionsAsync();
        var client = await _clientFactory.CreateTradingClientAsync(token);
        foreach (var action in actions) await ExecuteActionAsync(action, client, token);
    }

    private async Task ExecuteActionAsync(TradingAction action, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        var order = await PostOrderAsync(CreateRequestForAction(action), client, token);
        await _command.SaveActionWithAlpacaIdAsync(action, order.OrderId, token);
    }

    private static async Task<IOrder> PostOrderAsync(NewOrderRequest request, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        try
        {
            return await client.PostOrderAsync(request, token);
        }
        catch (RestClientErrorException e)
        {
            throw new UnsuccessfulAlpacaResponseException(e.HttpStatusCode is not null ? (int)e.HttpStatusCode : 0,
                e.Message);
        }
        catch (Exception e) when (e is HttpRequestException or SocketException or TaskCanceledException)
        {
            throw new AlpacaCallFailedException(e);
        }
    }

    private static NewOrderRequest CreateRequestForAction(TradingAction action)
    {
        var order = new NewOrderRequest(action.Symbol.Value, OrderQuantity.Fractional(action.Quantity),
            action.OrderType switch
            {
                OrderType.LimitSell => OrderSide.Sell,
                OrderType.LimitBuy => OrderSide.Buy,
                OrderType.MarketSell => OrderSide.Sell,
                OrderType.MarketBuy => OrderSide.Buy,
                _ => throw new ArgumentOutOfRangeException()
            }, action.OrderType switch
            {
                OrderType.LimitSell => Alpaca.Markets.OrderType.Limit,
                OrderType.LimitBuy => Alpaca.Markets.OrderType.Limit,
                OrderType.MarketSell => Alpaca.Markets.OrderType.Market,
                OrderType.MarketBuy => Alpaca.Markets.OrderType.Market,
                _ => throw new ArgumentOutOfRangeException()
            }, action.InForce);

        if (order.Type == Alpaca.Markets.OrderType.Limit) order.LimitPrice = action.Price;

        return order;
    }
}
