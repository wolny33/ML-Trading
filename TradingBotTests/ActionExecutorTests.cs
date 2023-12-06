using Alpaca.Markets;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using TradingBot.Exceptions;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Services.AlpacaClients;
using OrderType = Alpaca.Markets.OrderType;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class ActionExecutorTests
{
    private readonly Guid _actionId = Guid.NewGuid();
    private readonly ITradingActionCommand _command;
    private readonly ActionExecutor _executor;
    private readonly DateTimeOffset _now = new(2023, 12, 1, 20, 15, 0, TimeSpan.Zero);
    private readonly IStrategy _strategy;
    private readonly IAlpacaTradingClient _tradingClient = Substitute.For<IAlpacaTradingClient>();

    public ActionExecutorTests()
    {
        var orderResponse = Substitute.For<IOrder>();
        orderResponse.OrderId.Returns(_actionId);
        _tradingClient.PostOrderAsync(Arg.Any<NewOrderRequest>(), Arg.Any<CancellationToken>()).Returns(orderResponse);

        var clientFactory = Substitute.For<IAlpacaClientFactory>();
        clientFactory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClient);

        _strategy = Substitute.For<IStrategy>();
        _command = Substitute.For<ITradingActionCommand>();
        var logger = Substitute.For<ILogger>();
        _executor = new ActionExecutor(_strategy, clientFactory, _command, logger);
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
        await _command.Received(1).SaveActionWithAlpacaIdAsync(actions[0], _actionId, Arg.Any<CancellationToken>());
        await _command.Received(1).SaveActionWithAlpacaIdAsync(actions[1], _actionId, Arg.Any<CancellationToken>());
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
        await _command.Received(1).SaveActionWithErrorAsync(action,
            Arg.Is<Error>(e =>
                e.Code == "bad-alpaca-request" &&
                e.Message == "Validation failed for property 'quantity': quantity must be positive"),
            Arg.Any<CancellationToken>());
    }
}
