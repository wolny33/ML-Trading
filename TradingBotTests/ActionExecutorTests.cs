﻿using Alpaca.Markets;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using TradingBot.Exceptions;
using TradingBot.Models;
using TradingBot.Services;
using OrderType = Alpaca.Markets.OrderType;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class ActionExecutorTests : IAsyncDisposable
{
    private readonly Guid _actionId = Guid.NewGuid();
    private readonly IBacktestAssets _backtestAssets;
    private readonly ActionExecutor _executor;
    private readonly DateTimeOffset _now = new(2023, 12, 1, 20, 15, 0, TimeSpan.Zero);
    private readonly IStrategy _strategy;
    private readonly IAlpacaTradingClient _tradingClient = Substitute.For<IAlpacaTradingClient>();
    private readonly ICurrentTradingTask _tradingTask;

    public ActionExecutorTests()
    {
        var orderResponse = Substitute.For<IOrder>();
        orderResponse.OrderId.Returns(_actionId);
        _tradingClient.PostOrderAsync(Arg.Any<NewOrderRequest>(), Arg.Any<CancellationToken>()).Returns(orderResponse);

        var clientFactory = Substitute.For<IAlpacaClientFactory>();
        clientFactory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClient);

        _tradingTask = Substitute.For<ICurrentTradingTask>();
        _tradingTask.CurrentBacktestId.Returns((Guid?)null);

        _strategy = Substitute.For<IStrategy>();
        var logger = Substitute.For<ILogger>();
        var callQueue = new CallQueueMock();
        _backtestAssets = Substitute.For<IBacktestAssets>();
        _executor = new ActionExecutor(_strategy, clientFactory, logger, _tradingTask, callQueue, _backtestAssets);
    }

    public ValueTask DisposeAsync()
    {
        return _executor.DisposeAsync();
    }

    [Fact]
    public async Task ShouldCorrectlyPostOrder()
    {
        var actions = new[]
        {
            TradingAction.LimitBuy(new TradingSymbol("AMZN"), 12m, 123.45m, _now),
            TradingAction.MarketSell(new TradingSymbol("TQQQ"), 12m, _now)
        };
        _strategy.GetTradingActionsAsync().Returns(actions);

        await _executor.ExecuteTradingActionsAsync();

        await _tradingClient.Received(1).PostOrderAsync(Arg.Is<NewOrderRequest>(order =>
            order.Symbol == "AMZN" &&
            order.Quantity == OrderQuantity.Fractional(12m) &&
            order.Type == OrderType.Limit &&
            order.Side == OrderSide.Buy &&
            order.Duration == TimeInForce.Day &&
            order.LimitPrice == 123.45m
        ), Arg.Any<CancellationToken>());
        await _tradingClient.Received(1).PostOrderAsync(Arg.Is<NewOrderRequest>(order =>
            order.Symbol == "TQQQ" &&
            order.Quantity == OrderQuantity.Fractional(12m) &&
            order.Type == OrderType.Market &&
            order.Side == OrderSide.Sell &&
            order.Duration == TimeInForce.Day &&
            order.LimitPrice == null
        ), Arg.Any<CancellationToken>());
        await _tradingTask.Received(1)
            .SaveAndLinkSuccessfulActionAsync(actions[0], _actionId, Arg.Any<CancellationToken>());
        await _tradingTask.Received(1)
            .SaveAndLinkSuccessfulActionAsync(actions[1], _actionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldSaveErrorIfSomethingGoesWrong()
    {
        var action = TradingAction.MarketBuy(new TradingSymbol("AMZN"), -1m, _now);
        _strategy.GetTradingActionsAsync().Returns(new[] { action });
        _tradingClient.PostOrderAsync(Arg.Any<NewOrderRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new RequestValidationException("quantity must be positive", "quantity"));

        await _executor.ExecuteTradingActionsAsync();

        await _tradingClient.Received(1).PostOrderAsync(Arg.Is<NewOrderRequest>(order =>
            order.Symbol == "AMZN" &&
            order.Quantity == OrderQuantity.Fractional(-1m) &&
            order.Type == OrderType.Market &&
            order.Side == OrderSide.Buy &&
            order.Duration == TimeInForce.Day &&
            order.LimitPrice == null
        ), Arg.Any<CancellationToken>());
        await _tradingTask.Received(1).SaveAndLinkErroredActionAsync(action,
            Arg.Is<Error>(e =>
                e.Code == "bad-alpaca-request" &&
                e.Message == "Validation failed for property 'quantity': quantity must be positive"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldNotPostActionsToAlpacaInBacktest()
    {
        var actions = new[]
        {
            TradingAction.LimitBuy(new TradingSymbol("AMZN"), 12m, 123.45m, _now)
        };
        _strategy.GetTradingActionsAsync().Returns(actions);

        var backtestId = Guid.NewGuid();
        _tradingTask.CurrentBacktestId.Returns(backtestId);
        _tradingTask.GetTaskDay().Returns(new DateOnly(2024, 1, 1));

        await _executor.ExecuteTradingActionsAsync();

        await _backtestAssets.Received(1).PostActionForBacktestAsync(actions[0], backtestId, new DateOnly(2024, 1, 1));
        await _tradingTask.Received(1).SaveAndLinkBacktestActionAsync(actions[0], Arg.Any<CancellationToken>());

        await _tradingClient.DidNotReceive().PostOrderAsync(Arg.Any<NewOrderRequest>(), Arg.Any<CancellationToken>());
    }
}
