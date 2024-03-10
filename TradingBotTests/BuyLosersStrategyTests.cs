using FluentAssertions;
using NSubstitute;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Services.Strategy;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class BuyLosersStrategyTests
{
    private readonly IAssetsDataSource _assets;
    private readonly IMarketDataSource _marketData;
    private readonly IBuyLosersStrategyStateService _stateService;
    private readonly BuyLosersStrategy _strategy;

    public BuyLosersStrategyTests()
    {
        _assets = Substitute.For<IAssetsDataSource>();
        _marketData = Substitute.For<IMarketDataSource>();
        _stateService = Substitute.For<IBuyLosersStrategyStateService>();

        var tradingTask = Substitute.For<ICurrentTradingTask>();
        tradingTask.GetTaskDay().Returns(new DateOnly(2024, 3, 10));
        tradingTask.GetTaskTime().Returns(new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.Zero));
        tradingTask.CurrentBacktestId.Returns((Guid?)null);

        _strategy = new BuyLosersStrategy(tradingTask, _stateService, _marketData, _assets);
    }

    [Fact]
    public async Task ShouldDoNothingIfThereAreNoSymbolsToBuyAndItIsNotReevaluationDay()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            BacktestId = null,
            NextEvaluationDay = new DateOnly(2024, 3, 17),
            SymbolsToBuy = Array.Empty<TradingSymbol>()
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldBuyPendingSymbols()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            BacktestId = null,
            NextEvaluationDay = new DateOnly(2024, 3, 17),
            SymbolsToBuy = new[]
            {
                new TradingSymbol("AMZN"),
                new TradingSymbol("TSLA")
            }
        });

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 100m,
                AvailableAmount = 100m
            },
            Positions = new Dictionary<TradingSymbol, Position>()
        });

        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("AMZN"), Arg.Any<CancellationToken>())
            .Returns(95m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TSLA"), Arg.Any<CancellationToken>())
            .Returns(9.5m);

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(2).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("AMZN") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 0.5m
        ).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TSLA") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 5m
        );

        await _stateService.Received(1).ClearSymbolsToBuyAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public Task ShouldReevaluatePositionsAndSellUnwantedSymbols()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            BacktestId = null,
            NextEvaluationDay = new DateOnly(2024, 3, 10),
            SymbolsToBuy = Array.Empty<TradingSymbol>()
        });

        throw new NotImplementedException();
    }

    [Fact]
    public Task ShouldReevaluatePositionsOnFirstDay()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            BacktestId = null,
            NextEvaluationDay = null,
            SymbolsToBuy = Array.Empty<TradingSymbol>()
        });

        throw new NotImplementedException();
    }
}
