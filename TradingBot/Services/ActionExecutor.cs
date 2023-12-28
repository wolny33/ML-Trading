using System.Diagnostics;
using Alpaca.Markets;
using TradingBot.Exceptions;
using TradingBot.Exceptions.Alpaca;
using TradingBot.Models;
using ILogger = Serilog.ILogger;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Services;

public interface IActionExecutor
{
    Task ExecuteTradingActionsAsync(CancellationToken token = default);
    Task ExecuteActionAsync(TradingAction action, CancellationToken token = default);
}

public sealed class ActionExecutor : IActionExecutor
{
    private readonly IBacktestActionExecutor _backtestActionExecutor;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ILogger _logger;
    private readonly IStrategy _strategy;
    private readonly ICurrentTradingTask _tradingTask;

    public ActionExecutor(IStrategy strategy, IAlpacaClientFactory clientFactory, ILogger logger,
        ICurrentTradingTask tradingTask, IAlpacaCallQueue callQueue, IBacktestActionExecutor backtestActionExecutor)
    {
        _strategy = strategy;
        _clientFactory = clientFactory;
        _tradingTask = tradingTask;
        _callQueue = callQueue;
        _backtestActionExecutor = backtestActionExecutor;
        _logger = logger.ForContext<ActionExecutor>();
    }

    public async Task ExecuteTradingActionsAsync(CancellationToken token = default)
    {
        var actions = await _strategy.GetTradingActionsAsync(token);
        foreach (var action in actions) await ExecuteActionAndHandleErrorsAsync(action, token);
    }

    public async Task ExecuteActionAsync(TradingAction action, CancellationToken token = default)
    {
        if (_tradingTask.CurrentBacktestId is { } backtestId)
        {
            _backtestActionExecutor.PostActionForBacktest(action, backtestId);
            await _tradingTask.SaveAndLinkBacktestActionAsync(action, token);
            return;
        }

        var alpacaId = await PostActionToAlpacaAsync(action, token);
        await _tradingTask.SaveAndLinkSuccessfulActionAsync(action, alpacaId, token);
    }

    private async Task<Guid> PostActionToAlpacaAsync(TradingAction action, CancellationToken token)
    {
        _logger.Debug("Executing action {@Action}", action);
        using var client = await _clientFactory.CreateTradingClientAsync(token);
        var order = await _callQueue.SendRequestWithRetriesAsync(() =>
            PostOrderAsync(CreateRequestForAction(action), client, token)
                .ExecuteWithErrorHandling(_logger));
        return order.OrderId;
    }

    private async Task ExecuteActionAndHandleErrorsAsync(TradingAction action, CancellationToken token)
    {
        try
        {
            await ExecuteActionAsync(action, token);
        }
        catch (ResponseException e) when (e is BadAlpacaRequestException or InvalidFractionalOrderException
                                              or AssetNotFoundException or InsufficientAssetsException
                                              or InsufficientFundsException)
        {
            await _tradingTask.SaveAndLinkErroredActionAsync(action, e.GetError(), token);
        }
    }

    private async Task<IOrder> PostOrderAsync(NewOrderRequest request, IAlpacaTradingClient client,
        CancellationToken token = default)
    {
        try
        {
            return await client.PostOrderAsync(request, token);
        }
        catch (RestClientErrorException e) when (GetSpecialCaseException(e) is { } exception)
        {
            _logger.Warning(exception, "Alpaca request was invalid");
            throw exception;
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
