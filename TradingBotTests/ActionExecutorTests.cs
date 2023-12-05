using Alpaca.Markets;
using NSubstitute;
using Serilog;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Services.AlpacaClients;
using OrderType = Alpaca.Markets.OrderType;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class ActionExecutorTests
{
    private readonly Guid _actionId = Guid.NewGuid();
    private readonly IReadOnlyList<TradingAction> _actions;
    private readonly DateTimeOffset _now = new(2023, 12, 1, 20, 15, 0, TimeSpan.Zero);
    private readonly IAlpacaTradingClient _tradingClient = Substitute.For<IAlpacaTradingClient>();

    public ActionExecutorTests()
    {
        _actions = new[]
        {
            TradingAction.LimitBuy(new TradingSymbol("AMZN"), 12m, 123.45m, _now),
            TradingAction.MarketSell(new TradingSymbol("TQQQ"), 12m, _now)
        };

        var orderResponse = Substitute.For<IOrder>();
        orderResponse.OrderId.Returns(_actionId);
        _tradingClient.PostOrderAsync(Arg.Any<NewOrderRequest>(), Arg.Any<CancellationToken>()).Returns(orderResponse);
    }

    [Fact]
    public async Task ShouldCorrectlyPostOrder()
    {
        var strategy = Substitute.For<IStrategy>();
        strategy.GetTradingActionsAsync().Returns(_actions);

        var clientFactory = Substitute.For<IAlpacaClientFactory>();
        clientFactory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClient);

        var command = Substitute.For<ITradingActionCommand>();

        var actionExecutor = new ActionExecutor(strategy, clientFactory, command, Substitute.For<ILogger>());

        await actionExecutor.ExecuteTradingActionsAsync();

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
        await command.Received(1).SaveActionWithAlpacaIdAsync(_actions[0], _actionId, Arg.Any<CancellationToken>());
        await command.Received(1).SaveActionWithAlpacaIdAsync(_actions[1], _actionId, Arg.Any<CancellationToken>());
    }
}
