using FluentAssertions;
using NSubstitute;
using TradingBot.Configuration;
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

        var strategyParameters = Substitute.For<IStrategyParametersService>();
        strategyParameters.GetConfigurationAsync(Arg.Any<CancellationToken>()).Returns(
            new StrategyParametersConfiguration
            {
                LimitPriceDamping = 0.5m,
                BuyLosers = new BuyLosersOptions
                {
                    AnalysisLengthInDays = 30,
                    EvaluationFrequencyInDays = 30
                },
                Basic = null!,
                BuyWinners = null!,
                Pca = null!
            });

        _strategy = new BuyLosersStrategy(tradingTask, _stateService, _marketData, _assets, strategyParameters);
    }

    [Fact]
    public async Task ShouldDoNothingIfThereAreNoSymbolsToBuyAndItIsNotReevaluationDay()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 3, 17),
            SymbolsToBuy = Array.Empty<TradingSymbol>()
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldBuyPendingSymbolsAndClearAlreadyOwned()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 3, 17),
            SymbolsToBuy = new[]
            {
                new TradingSymbol("AMZN"),
                new TradingSymbol("TSLA"),
                new TradingSymbol("TQQQ")
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
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TQQQ")] = new()
                {
                    Symbol = new TradingSymbol("TQQQ"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1,
                    AvailableQuantity = 1,
                    MarketValue = 100,
                    AverageEntryPrice = 90
                }
            }
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

        await _stateService.Received(1)
            .ClearSymbolToBuyAsync(new TradingSymbol("TQQQ"), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReevaluatePositionsAndSellUnwantedSymbols()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 3, 10),
            SymbolsToBuy = Array.Empty<TradingSymbol>()
        });

        _marketData.GetPricesForAllSymbolsAsync(new DateOnly(2024, 3, 10).AddDays(-30), new DateOnly(2024, 3, 10),
            Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(0, 20).Select(n =>
                    Enumerable.Range(0, 31).Select(k => new DailyTradingData
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(k - 30),
                        Open = 100m + 0.1m * (n - 10m) * k,
                        Close = 100m + 0.1m * (n - 10m) * (k + 1),
                        High = 102m + 0.1m * (n - 10m) * k,
                        Low = 98m + 0.1m * (n - 10m) * k,
                        Volume = 10_000m
                    }).ToList()
                ).Select((data, index) => (Data: data, Index: index))
                .ToDictionary(pair => new TradingSymbol($"TKN{pair.Index}"),
                    pair => pair.Data as IReadOnlyList<DailyTradingData>)
        );

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 200m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 0m,
                AvailableAmount = 0m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN0")] = new()
                {
                    Symbol = new TradingSymbol("TKN0"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 90m
                },
                [new TradingSymbol("TKN3")] = new()
                {
                    Symbol = new TradingSymbol("TKN3"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 95m
                }
            }
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(1).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN3") &&
            a.OrderType == OrderType.MarketSell &&
            a.Quantity == 1m);

        await _stateService.Received(1)
            .SetSymbolsToBuyAsync(
                Arg.Is<IReadOnlyList<TradingSymbol>>(list => list.Count == 1 && list[0] == new TradingSymbol("TKN1")),
                null, Arg.Any<CancellationToken>());

        await _stateService.Received(1)
            .SetNextExecutionDayAsync(new DateOnly(2024, 3, 10).AddDays(30), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReevaluatePositionsOnFirstDay()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyLosersStrategyState
        {
            NextEvaluationDay = null,
            SymbolsToBuy = Array.Empty<TradingSymbol>()
        });

        _marketData.GetPricesForAllSymbolsAsync(new DateOnly(2024, 3, 10).AddDays(-30), new DateOnly(2024, 3, 10),
            Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(0, 20).Select(n =>
                    Enumerable.Range(0, 31).Select(k => new DailyTradingData
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(k - 30),
                        Open = 100m + 0.1m * (n - 10m) * k,
                        Close = 100m + 0.1m * (n - 10m) * (k + 1),
                        High = 102m + 0.1m * (n - 10m) * k,
                        Low = 98m + 0.1m * (n - 10m) * k,
                        Volume = 10_000m
                    }).ToList()
                ).Select((data, index) => (Data: data, Index: index))
                .ToDictionary(pair => new TradingSymbol($"TKN{pair.Index}"),
                    pair => pair.Data as IReadOnlyList<DailyTradingData>)
        );

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 200m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 0m,
                AvailableAmount = 0m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN0")] = new()
                {
                    Symbol = new TradingSymbol("TKN0"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 90m
                },
                [new TradingSymbol("TKN3")] = new()
                {
                    Symbol = new TradingSymbol("TKN3"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 95m
                }
            }
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(1).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN3") &&
            a.OrderType == OrderType.MarketSell &&
            a.Quantity == 1m);

        await _stateService.Received(1)
            .SetSymbolsToBuyAsync(
                Arg.Is<IReadOnlyList<TradingSymbol>>(list => list.Count == 1 && list[0] == new TradingSymbol("TKN1")),
                null, Arg.Any<CancellationToken>());

        await _stateService.Received(1)
            .SetNextExecutionDayAsync(new DateOnly(2024, 3, 10).AddDays(30), null, Arg.Any<CancellationToken>());
    }
}
