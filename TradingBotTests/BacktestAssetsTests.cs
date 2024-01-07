using System.Collections.Immutable;
using Alpaca.Markets;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using TradingBot.Exceptions.Alpaca;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class BacktestAssetsTests
{
    private readonly BacktestAssets _backtestAssets;
    private readonly Guid _backtestId = Guid.NewGuid();
    private readonly IMarketDataSource _marketDataSource;
    private readonly DateTimeOffset _now = new(2024, 1, 1, 15, 40, 0, TimeSpan.Zero);
    private readonly ITradingActionCommand _tradingActionCommand;

    public BacktestAssetsTests()
    {
        _tradingActionCommand = Substitute.For<ITradingActionCommand>();
        _marketDataSource = Substitute.For<IMarketDataSource>();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.GetService(typeof(IMarketDataSource)).Returns(_marketDataSource);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var logger = Substitute.For<ILogger>();
        logger.ForContext<object>().Returns(logger);

        _backtestAssets = new BacktestAssets(scopeFactory, _tradingActionCommand, logger);
        _backtestAssets.InitializeForId(_backtestId, 100m);
    }

    [Fact]
    public async Task ShouldReserveCashWhenPostingAction()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitBuy(new TradingSymbol("TKN"), 1m, 2m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 100m,
                BuyingPower = 98m
            },
            Positions = ImmutableDictionary<TradingSymbol, Position>.Empty
        });
    }

    [Fact]
    public async Task ShouldNotReserveCashWhenPostingMarketBuyActionForSymbolWithMissingMarketData()
    {
        _marketDataSource
            .GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2), new DateOnly(2024, 1, 2),
                Arg.Any<CancellationToken>()).Returns((IReadOnlyList<DailyTradingData>?)null);

        var action = TradingAction.MarketBuy(new TradingSymbol("TKN"), 1m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 100m,
                BuyingPower = 100m
            },
            Positions = ImmutableDictionary<TradingSymbol, Position>.Empty
        });
    }

    [Fact]
    public async Task ShouldReserveAssetsWhenPostingAction()
    {
        var assets = new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 0m,
                BuyingPower = 0m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN")] = new()
                {
                    Symbol = new TradingSymbol("TKN"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 100m
                }
            }
        };
        _backtestAssets.SetAssetsForBacktest(_backtestId, assets);

        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitSell(new TradingSymbol("TKN"), 1m, 200m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 0m,
                BuyingPower = 0m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN")] = new()
                {
                    SymbolId = assets.Positions[new TradingSymbol("TKN")].SymbolId,
                    Symbol = new TradingSymbol("TKN"),
                    Quantity = 1m,
                    AvailableQuantity = 0m,
                    MarketValue = 100m,
                    AverageEntryPrice = 100m
                }
            }
        });
    }

    [Fact]
    public async Task ShouldThrowIfQuantityIsNonPositive()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitBuy(new TradingSymbol("TKN"), -1m, 2m, _now);
        await _backtestAssets
            .Awaiting(assets => assets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1)))
            .Should().ThrowAsync<BadAlpacaRequestException>();
    }

    [Fact]
    public async Task ShouldThrowIfFractionalOrderIsIncorrect()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitBuy(new TradingSymbol("TKN"), 0.1m, 2m, _now);
        await _backtestAssets
            .Awaiting(assets => assets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1)))
            .Should().ThrowAsync<InvalidFractionalOrderException>();
    }

    [Fact]
    public async Task ShouldThrowWhenAttemptingToSellUnownedAssets()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitSell(new TradingSymbol("TKN"), 1m, 2m, _now);
        await _backtestAssets
            .Awaiting(assets => assets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1)))
            .Should().ThrowAsync<InsufficientAssetsException>();
    }

    [Fact]
    public async Task ShouldThrowIfBuyingPowerIsInsufficientForLimitBuy()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitBuy(new TradingSymbol("TKN"), 100m, 2m, _now);
        await _backtestAssets
            .Awaiting(assets => assets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1)))
            .Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task ShouldThrowIfBuyingPowerIsInsufficientForMarketBuy()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.MarketBuy(new TradingSymbol("TKN"), 100m, _now);
        await _backtestAssets
            .Awaiting(assets => assets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1)))
            .Should().ThrowAsync<InsufficientFundsException>();
    }

    [Fact]
    public async Task ShouldExpireActionIfMarketDataIsMissing()
    {
        _marketDataSource
            .GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2), new DateOnly(2024, 1, 2),
                Arg.Any<CancellationToken>()).Returns((IReadOnlyList<DailyTradingData>?)null);

        var action = TradingAction.MarketBuy(new TradingSymbol("TKN"), 1m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(_backtestId, new DateOnly(2024, 1, 2));
        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 100m,
                BuyingPower = 100m
            },
            Positions = ImmutableDictionary<TradingSymbol, Position>.Empty
        });

        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(action.Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Expired && s.FillPrice == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldExpireLimitActionIfPriceDoesNotReachRequestedLimit()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitBuy(new TradingSymbol("TKN"), 10m, 0.5m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(_backtestId, new DateOnly(2024, 1, 2));
        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 100m,
                BuyingPower = 100m
            },
            Positions = ImmutableDictionary<TradingSymbol, Position>.Empty
        });

        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(action.Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Expired && s.FillPrice == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCorrectlyExecuteMarketBuyAction()
    {
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.MarketBuy(new TradingSymbol("TKN"), 1m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(_backtestId, new DateOnly(2024, 1, 2));
        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 100m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 98m,
                BuyingPower = 98m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN")] = new()
                {
                    Symbol = new TradingSymbol("TKN"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    AverageEntryPrice = 2m,
                    MarketValue = 2m
                }
            }
        }, options => options.For(a => a.Positions).Exclude(p => p.Value.SymbolId));

        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(action.Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Filled && s.FillPrice == 2m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCorrectlyExecuteLimitSellAction()
    {
        var assets = new Assets
        {
            EquityValue = 102m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 100m,
                BuyingPower = 100m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN")] = new()
                {
                    Symbol = new TradingSymbol("TKN"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    AverageEntryPrice = 2m,
                    MarketValue = 2m
                }
            }
        };
        _backtestAssets.SetAssetsForBacktest(_backtestId, assets);

        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });

        var action = TradingAction.LimitSell(new TradingSymbol("TKN"), 1m, 3.5m, _now);
        await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(_backtestId, new DateOnly(2024, 1, 2));
        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 103.5m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 103.5m,
                BuyingPower = 103.5m
            },
            Positions = ImmutableDictionary<TradingSymbol, Position>.Empty
        });

        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(action.Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Filled && s.FillPrice == 3.5m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCorrectlyExecuteSeveralActions()
    {
        var assets = new Assets
        {
            EquityValue = 118m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 100m,
                BuyingPower = 100m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN1")] = new()
                {
                    Symbol = new TradingSymbol("TKN1"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 3m,
                    AvailableQuantity = 3m,
                    AverageEntryPrice = 2m,
                    MarketValue = 6m
                },
                [new TradingSymbol("TKN2")] = new()
                {
                    Symbol = new TradingSymbol("TKN2"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    AverageEntryPrice = 11m,
                    MarketValue = 11m
                }
            }
        };
        _backtestAssets.SetAssetsForBacktest(_backtestId, assets);

        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN1"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });
        _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN2"), new DateOnly(2024, 1, 2),
            new DateOnly(2024, 1, 2), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2024, 1, 2),
                Open = 12m,
                Close = 13m,
                High = 14m,
                Low = 11m,
                Volume = 100m
            }
        });

        var actions = new[]
        {
            TradingAction.LimitSell(new TradingSymbol("TKN1"), 1m, 3.5m, _now),
            TradingAction.MarketSell(new TradingSymbol("TKN1"), 1m, _now),
            TradingAction.LimitBuy(new TradingSymbol("TKN2"), 1m, 10m, _now),
            TradingAction.MarketBuy(new TradingSymbol("TKN2"), 1m, _now)
        };
        foreach (var action in actions)
            await _backtestAssets.PostActionForBacktestAsync(action, _backtestId, new DateOnly(2024, 1, 1));

        await _backtestAssets.ExecuteQueuedActionsForBacktestAsync(_backtestId, new DateOnly(2024, 1, 2));
        _backtestAssets.GetForBacktestWithId(_backtestId).Should().BeEquivalentTo(new Assets
        {
            EquityValue = 121m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                AvailableAmount = 93.5m,
                BuyingPower = 93.5m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN1")] = new()
                {
                    Symbol = new TradingSymbol("TKN1"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    AverageEntryPrice = 2m,
                    MarketValue = 3.5m
                },
                [new TradingSymbol("TKN2")] = new()
                {
                    Symbol = new TradingSymbol("TKN2"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 2m,
                    AvailableQuantity = 2m,
                    AverageEntryPrice = 11.5m,
                    MarketValue = 24m
                }
            }
        }, options => options.For(a => a.Positions).Exclude(p => p.Value.SymbolId));

        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(actions[0].Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Filled && s.FillPrice == 3.5m),
            Arg.Any<CancellationToken>());
        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(actions[1].Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Filled && s.FillPrice == 2m),
            Arg.Any<CancellationToken>());
        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(actions[2].Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Expired && s.FillPrice == null),
            Arg.Any<CancellationToken>());
        await _tradingActionCommand.Received(1).UpdateBacktestActionStateAsync(actions[3].Id,
            Arg.Is<BacktestActionState>(s => s.Status == OrderStatus.Filled && s.FillPrice == 12m),
            Arg.Any<CancellationToken>());
    }
}
