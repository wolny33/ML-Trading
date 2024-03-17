using Alpaca.Markets;
using FluentAssertions;
using NSubstitute;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Services.Strategy;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class BuyWinnersStrategyTests
{
    private readonly ITradingActionQuery _actionQuery;
    private readonly IAssetsDataSource _assets;
    private readonly IMarketDataSource _marketData;
    private readonly IBuyWinnersStrategyStateService _stateService;
    private readonly BuyWinnersStrategy _strategy;

    public BuyWinnersStrategyTests()
    {
        _assets = Substitute.For<IAssetsDataSource>();
        _marketData = Substitute.For<IMarketDataSource>();
        _stateService = Substitute.For<IBuyWinnersStrategyStateService>();
        _actionQuery = Substitute.For<ITradingActionQuery>();

        var tradingTask = Substitute.For<ICurrentTradingTask>();
        tradingTask.GetTaskDay().Returns(new DateOnly(2024, 3, 10));
        tradingTask.GetTaskTime().Returns(new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.Zero));
        tradingTask.CurrentBacktestId.Returns((Guid?)null);

        _strategy = new BuyWinnersStrategy(tradingTask, _stateService, _marketData, _assets, _actionQuery);
    }

    [Fact]
    public async Task ShouldBuyPendingSymbolsIfThereIsWaitingEvaluation()
    {
        var pendingEvaluationId = Guid.NewGuid();
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyWinnersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 4, 1),
            Evaluations = new[]
            {
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 1, 1),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(0, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 2, 1),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(3, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = pendingEvaluationId,
                    CreatedAt = new DateOnly(2024, 3, 1),
                    Bought = false,
                    ActionIds = Array.Empty<Guid>(),
                    SymbolsToBuy = Enumerable.Range(6, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                }
            }
        });

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 300m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 300m,
                AvailableAmount = 300m
            },
            Positions = new Dictionary<TradingSymbol, Position>()
        });

        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN6"), Arg.Any<CancellationToken>())
            .Returns(95m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN7"), Arg.Any<CancellationToken>())
            .Returns(190m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN8"), Arg.Any<CancellationToken>())
            .Returns(380m);

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(3).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN6") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 1m
        ).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN7") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 0.5m
        ).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN8") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 0.25m);

        await _stateService.Received(1).SaveActionIdsForEvaluationAsync(
            Arg.Is<IReadOnlyList<Guid>>(list => list.Count == 3),
            pendingEvaluationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldNotUseUpAllMoneyIfThereAreLessThanThreeActiveEvaluations()
    {
        var pendingEvaluationId = Guid.NewGuid();
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyWinnersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 4, 1),
            Evaluations = new[]
            {
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 2, 1),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(3, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = pendingEvaluationId,
                    CreatedAt = new DateOnly(2024, 3, 1),
                    Bought = false,
                    ActionIds = Array.Empty<Guid>(),
                    SymbolsToBuy = Enumerable.Range(6, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                }
            }
        });

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 600m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 600m,
                AvailableAmount = 600m
            },
            Positions = new Dictionary<TradingSymbol, Position>()
        });

        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN6"), Arg.Any<CancellationToken>())
            .Returns(95m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN7"), Arg.Any<CancellationToken>())
            .Returns(190m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN8"), Arg.Any<CancellationToken>())
            .Returns(380m);

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(3).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN6") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 1m
        ).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN7") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 0.5m
        ).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN8") &&
            a.OrderType == OrderType.MarketBuy &&
            a.Quantity == 0.25m);

        await _stateService.Received(1).SaveActionIdsForEvaluationAsync(
            Arg.Is<IReadOnlyList<Guid>>(list => list.Count == 3),
            pendingEvaluationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldNotBuySymbolsIfEvaluationIsLessThanSevenDaysOld()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyWinnersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 4, 9),
            Evaluations = new[]
            {
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 1, 9),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(0, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 2, 9),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(3, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 3, 9),
                    Bought = false,
                    ActionIds = Array.Empty<Guid>(),
                    SymbolsToBuy = Enumerable.Range(6, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                }
            }
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldDoNothingIfItIsNotEvaluationDayAndThereAreNoPendingEvaluations()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyWinnersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 4, 8),
            Evaluations = new[]
            {
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 1, 8),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(0, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 2, 8),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(3, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 3, 8),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(6, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                }
            }
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldCreateNewEvaluationAndSellExpiredOnesOnEvaluationDay()
    {
        var expiredEvaluationId = Guid.NewGuid();
        var actionIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyWinnersStrategyState
        {
            NextEvaluationDay = new DateOnly(2024, 3, 9),
            Evaluations = new[]
            {
                new BuyWinnersEvaluation
                {
                    Id = expiredEvaluationId,
                    CreatedAt = new DateOnly(2023, 12, 10),
                    Bought = true,
                    ActionIds = actionIds,
                    SymbolsToBuy = Enumerable.Range(0, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 1, 10),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(3, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                },
                new BuyWinnersEvaluation
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = new DateOnly(2024, 2, 10),
                    Bought = true,
                    ActionIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToList(),
                    SymbolsToBuy = Enumerable.Range(6, 3).Select(n => new TradingSymbol($"TKN{n}")).ToList()
                }
            }
        });

        _marketData.GetPricesForAllSymbolsAsync(new DateOnly(2024, 3, 10).AddDays(-12 * 30), new DateOnly(2024, 3, 10),
            Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(0, 20).Select(n =>
                    Enumerable.Range(0, 12 * 30 + 1).Select(k => new DailyTradingData
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(k - 12 * 30),
                        Open = 100m + 0.01m * (10m - n) * k,
                        Close = 100m + 0.01m * (10m - n) * (k + 1),
                        High = 102m + 0.01m * (10m - n) * k,
                        Low = 98m + 0.01m * (10m - n) * k,
                        Volume = 10_000m
                    }).ToList()
                ).Select((data, index) => (Data: data, Index: index))
                .ToDictionary(pair => new TradingSymbol($"TKN{pair.Index}"),
                    pair => pair.Data as IReadOnlyList<DailyTradingData>)
        );

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 600m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 600m,
                AvailableAmount = 600m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN2")] = new()
                {
                    Symbol = new TradingSymbol("TKN2"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 90m,
                    AverageEntryPrice = 100m
                }
            }
        });

        _actionQuery.GetLatestTradingActionStateByIdAsync(actionIds[0], Arg.Any<CancellationToken>())
            .Returns((TradingAction?)null);
        _actionQuery.GetLatestTradingActionStateByIdAsync(actionIds[1], Arg.Any<CancellationToken>())
            .Returns(new TradingAction
            {
                Id = actionIds[1],
                AlpacaId = Guid.NewGuid(),
                Symbol = new TradingSymbol("TKN1"),
                OrderType = OrderType.MarketBuy,
                Quantity = 1m,
                Price = null,
                InForce = TimeInForce.Day,
                CreatedAt = new DateTimeOffset(2023, 12, 11, 12, 0, 0, TimeSpan.Zero),
                ExecutedAt = new DateTimeOffset(2023, 12, 11, 13, 0, 0, TimeSpan.Zero),
                Status = OrderStatus.Canceled,
                Error = null,
                AverageFillPrice = null
            });
        _actionQuery.GetLatestTradingActionStateByIdAsync(actionIds[2], Arg.Any<CancellationToken>())
            .Returns(new TradingAction
            {
                Id = actionIds[2],
                AlpacaId = Guid.NewGuid(),
                Symbol = new TradingSymbol("TKN2"),
                OrderType = OrderType.MarketBuy,
                Quantity = 1m,
                Price = null,
                InForce = TimeInForce.Day,
                CreatedAt = new DateTimeOffset(2023, 12, 11, 12, 0, 0, TimeSpan.Zero),
                ExecutedAt = new DateTimeOffset(2023, 12, 11, 15, 0, 0, TimeSpan.Zero),
                Status = OrderStatus.Filled,
                Error = null,
                AverageFillPrice = 100m
            });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(1).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN2") &&
            a.OrderType == OrderType.MarketSell &&
            a.Quantity == 1m);

        await _stateService.Received(1)
            .SetNextExecutionDayAsync(new DateOnly(2024, 4, 8), null, Arg.Any<CancellationToken>());
        await _stateService.Received(1).SaveNewEvaluationAsync(Arg.Is<BuyWinnersEvaluation>(e =>
            e.CreatedAt == new DateOnly(2024, 3, 10) &&
            e.Bought == false &&
            e.ActionIds.Count == 0 &&
            e.SymbolsToBuy.Count == 2 &&
            e.SymbolsToBuy.Contains(new TradingSymbol("TKN0")) &&
            e.SymbolsToBuy.Contains(new TradingSymbol("TKN1"))), null, Arg.Any<CancellationToken>());
        await _stateService.Received(1).DeleteEvaluationAsync(expiredEvaluationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldPerformEvaluationOnFirstDay()
    {
        _stateService.GetStateAsync(null, Arg.Any<CancellationToken>()).Returns(new BuyWinnersStrategyState
        {
            NextEvaluationDay = null,
            Evaluations = Array.Empty<BuyWinnersEvaluation>()
        });

        _marketData.GetPricesForAllSymbolsAsync(new DateOnly(2024, 3, 10).AddDays(-12 * 30), new DateOnly(2024, 3, 10),
            Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(0, 20).Select(n =>
                    Enumerable.Range(0, 12 * 30 + 1).Select(k => new DailyTradingData
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(k - 12 * 30),
                        Open = 100m + 0.01m * (10m - n) * k,
                        Close = 100m + 0.01m * (10m - n) * (k + 1),
                        High = 102m + 0.01m * (10m - n) * k,
                        Low = 98m + 0.01m * (10m - n) * k,
                        Volume = 10_000m
                    }).ToList()
                ).Select((data, index) => (Data: data, Index: index))
                .ToDictionary(pair => new TradingSymbol($"TKN{pair.Index}"),
                    pair => pair.Data as IReadOnlyList<DailyTradingData>)
        );

        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 600m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 600m,
                AvailableAmount = 600m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN2")] = new()
                {
                    Symbol = new TradingSymbol("TKN2"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 90m,
                    AverageEntryPrice = 100m
                }
            }
        });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().HaveCount(1).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TKN2") &&
            a.OrderType == OrderType.MarketSell &&
            a.Quantity == 1m);

        await _stateService.Received(1)
            .SetNextExecutionDayAsync(new DateOnly(2024, 4, 9), null, Arg.Any<CancellationToken>());
        await _stateService.Received(1).SaveNewEvaluationAsync(Arg.Is<BuyWinnersEvaluation>(e =>
            e.CreatedAt == new DateOnly(2024, 3, 10) &&
            e.Bought == false &&
            e.ActionIds.Count == 0 &&
            e.SymbolsToBuy.Count == 2 &&
            e.SymbolsToBuy.Contains(new TradingSymbol("TKN0")) &&
            e.SymbolsToBuy.Contains(new TradingSymbol("TKN1"))), null, Arg.Any<CancellationToken>());
    }
}
