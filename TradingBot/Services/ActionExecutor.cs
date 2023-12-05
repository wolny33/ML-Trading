using System.Diagnostics;
using System.Net.Sockets;
using Alpaca.Markets;
using TradingBot.Exceptions;
using TradingBot.Exceptions.Alpaca;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;
using ILogger = Serilog.ILogger;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface IActionExecutor
{
    public Task ExecuteTradingActionsAsync(CancellationToken token = default);

    Task ExecuteActionAsync(TradingAction action, CancellationToken token = default);
}

public sealed class ActionExecutor : IActionExecutor
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ITradingActionCommand _command;
    private readonly ILogger _logger;
    private readonly IStrategy _strategy;

    public ActionExecutor(IStrategy strategy, IAlpacaClientFactory clientFactory, ITradingActionCommand command,
        ILogger logger)
    {
        _strategy = strategy;
        _clientFactory = clientFactory;
        _command = command;
        _logger = logger.ForContext<ActionExecutor>();
    }

    public async Task ExecuteTradingActionsAsync(CancellationToken token = default)
    {
        var actions = await _strategy.GetTradingActionsAsync();
        foreach (var action in actions) await ExecuteActionAndHandleErrorsAsync(action, token);
    }

    public async Task ExecuteActionAsync(TradingAction action, CancellationToken token = default)
    {
        _logger.Debug("Executing action {@Action}", action);
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var order = await PostOrderAsync(CreateRequestForAction(action), client, token);
        await _command.SaveActionWithAlpacaIdAsync(action, order.OrderId, token);
    }

    private async Task ExecuteActionAndHandleErrorsAsync(TradingAction action, CancellationToken token)
    {
        try
        {
            await ExecuteActionAsync(action, token);
        }
        catch (Exception e) when (e is BadAlpacaRequestException or InvalidFractionalOrderException
                                      or AssetNotFoundException or InsufficientAssetsException
                                      or InsufficientFundsException)
        {
        }
    }

    private async Task<IOrder> PostOrderAsync(NewOrderRequest request, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        try
        {
            return await client.PostOrderAsync(request, token);
        }
        catch (RequestValidationException e)
        {
            _logger.Warning(e, "Alpaca request failed validation");
            throw new BadAlpacaRequestException(e);
        }
        catch (RestClientErrorException e) when (GetSpecialCaseException(e) is { } exception)
        {
            _logger.Warning(exception, "Alpaca request was invalid");
            throw exception;
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            _logger.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            _logger.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
    }

    private static UnsuccessfulAlpacaResponseException? GetSpecialCaseException(RestClientErrorException exception)
    {
        return InvalidFractionalOrderException.FromWrapperException(exception) ??
               AssetNotFoundException.FromWrapperException(exception) ??
               InsufficientAssetsException.FromWrapperException(exception) ??
               InsufficientFundsException.FromWrapperException(exception) as UnsuccessfulAlpacaResponseException;
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
                _ => throw new UnreachableException($"Switch on {nameof(OrderType)} should be exhaustive")
            }, action.OrderType switch
            {
                OrderType.LimitSell => Alpaca.Markets.OrderType.Limit,
                OrderType.LimitBuy => Alpaca.Markets.OrderType.Limit,
                OrderType.MarketSell => Alpaca.Markets.OrderType.Market,
                OrderType.MarketBuy => Alpaca.Markets.OrderType.Market,
                _ => throw new UnreachableException($"Switch on {nameof(OrderType)} should be exhaustive")
            }, action.InForce);

        if (order.Type == Alpaca.Markets.OrderType.Limit) order.LimitPrice = action.Price;

        return order;
    }
}
