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
}

public sealed class ActionExecutor : IActionExecutor, IAsyncDisposable
{
    private readonly IBacktestAssets _backtestAssets;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly ILogger _logger;
    private readonly IStrategy _strategy;
    private readonly Lazy<Task<IAlpacaTradingClient>> _tradingClient;
    private readonly ICurrentTradingTask _tradingTask;

    public ActionExecutor(IStrategy strategy, IAlpacaClientFactory clientFactory, ILogger logger,
        ICurrentTradingTask tradingTask, IAlpacaCallQueue callQueue, IBacktestAssets backtestAssets)
    {
        _strategy = strategy;
        _tradingTask = tradingTask;
        _callQueue = callQueue;
        _backtestAssets = backtestAssets;
        _logger = logger.ForContext<ActionExecutor>();
        _tradingClient = new Lazy<Task<IAlpacaTradingClient>>(() => clientFactory.CreateTradingClientAsync());
    }

    public async Task ExecuteTradingActionsAsync(CancellationToken token = default)
    {
        var actions = await _strategy.GetTradingActionsAsync(token);
        foreach (var action in actions) await ExecuteActionAndHandleErrorsAsync(action, token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_tradingClient.IsValueCreated) (await _tradingClient.Value).Dispose();
    }

    private async Task ExecuteActionAsync(TradingAction action, CancellationToken token = default)
    {
        if (_tradingTask.CurrentBacktestId is { } backtestId)
        {
            await _backtestAssets.PostActionForBacktestAsync(action, backtestId, _tradingTask.GetTaskDay());
            await _tradingTask.SaveAndLinkBacktestActionAsync(action, token);
            return;
        }

        var alpacaId = await PostActionToAlpacaAsync(action, token);
        await _tradingTask.SaveAndLinkSuccessfulActionAsync(action, alpacaId, token);
    }

    private async Task<Guid> PostActionToAlpacaAsync(TradingAction action, CancellationToken token)
    {
        _logger.Debug("Executing action {@Action}", action);
        var client = await _tradingClient.Value;
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
