using FluentAssertions;
using NSubstitute;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Services.Strategy;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class GreedyStrategyTests
{
    private readonly IAssetsDataSource _assets;
    private readonly IMarketDataSource _marketData;
    private readonly IPricePredictor _predictor;
    private readonly GreedyStrategy _strategy;

    public GreedyStrategyTests()
    {
        _assets = Substitute.For<IAssetsDataSource>();
        _marketData = Substitute.For<IMarketDataSource>();
        _predictor = Substitute.For<IPricePredictor>();

        var tradingTask = Substitute.For<ICurrentTradingTask>();
        tradingTask.GetTaskDay().Returns(new DateOnly(2024, 3, 10));
        tradingTask.GetTaskTime().Returns(new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.Zero));
        tradingTask.CurrentBacktestId.Returns((Guid?)null);

        _strategy = new GreedyStrategy(_assets, _marketData, _predictor, tradingTask);
    }

    [Fact]
    public async Task ShouldSellEverythingIfHoldsMultipleStocks()
    {
        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 600m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 100m,
                AvailableAmount = 100m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    Symbol = new TradingSymbol("AMZN"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 100m
                },
                [new TradingSymbol("TSLA")] = new()
                {
                    Symbol = new TradingSymbol("TSLA"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 2m,
                    AvailableQuantity = 2m,
                    MarketValue = 400m,
                    AverageEntryPrice = 400m
                }
            }
        });

        _predictor.GetPredictionsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TradingSymbol, Prediction>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    Prices = new[]
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 11),
                            ClosingPrice = 100m,
                            HighPrice = 110m,
                            LowPrice = 90m
                        }
                    }
                },
                [new TradingSymbol("TSLA")] = new()
                {
                    Prices = new[]
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 11),
                            ClosingPrice = 200m,
                            HighPrice = 210m,
                            LowPrice = 190m
                        }
                    }
                }
            });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().ContainSingle(a =>
            a.Symbol == new TradingSymbol("AMZN") &&
            a.Quantity == 1m &&
            a.OrderType == OrderType.LimitSell &&
            a.Price == 110m).And.ContainSingle(a =>
            a.Symbol == new TradingSymbol("TSLA") &&
            a.Quantity == 2m &&
            a.OrderType == OrderType.LimitSell &&
            a.Price == 210m
        ).And.HaveCount(2);
    }

    [Fact]
    public async Task ShouldReturnCorrectActionIfHoldsMoney()
    {
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
            .Returns(100m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TSLA"), Arg.Any<CancellationToken>())
            .Returns(100m);

        _predictor.GetPredictionsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TradingSymbol, Prediction>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    Prices = Enumerable.Range(1, 5).Select(n => new DailyPricePrediction
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(n),
                        ClosingPrice = 100m + n * 5m,
                        HighPrice = 105m + n * 5m,
                        LowPrice = 95m + n * 5m
                    }).ToList()
                },
                [new TradingSymbol("TSLA")] = new()
                {
                    Prices = Enumerable.Range(1, 5).Select(n => new DailyPricePrediction
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(n),
                        ClosingPrice = 100m + n * 10m,
                        HighPrice = 110m + n * 10m,
                        LowPrice = 90m + n * 10m
                    }).ToList()
                }
            });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().ContainSingle(a =>
            a.Symbol == new TradingSymbol("TSLA") &&
            a.Quantity == 1m &&
            a.OrderType == OrderType.LimitBuy &&
            a.Price == 100m
        ).And.HaveCount(1);
    }

    [Fact]
    public async Task ShouldReturnCorrectActionIfHoldsStock()
    {
        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 0m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 0m,
                AvailableAmount = 0m
            },
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    Symbol = new TradingSymbol("AMZN"),
                    SymbolId = Guid.NewGuid(),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    MarketValue = 100m,
                    AverageEntryPrice = 100m
                }
            }
        });

        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("AMZN"), Arg.Any<CancellationToken>())
            .Returns(100m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TSLA"), Arg.Any<CancellationToken>())
            .Returns(100m);

        _predictor.GetPredictionsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TradingSymbol, Prediction>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    Prices = Enumerable.Range(1, 5).Select(n => new DailyPricePrediction
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(n),
                        ClosingPrice = 100m + n * 5m,
                        HighPrice = 105m + n * 5m,
                        LowPrice = 95m + n * 5m
                    }).ToList()
                },
                [new TradingSymbol("TSLA")] = new()
                {
                    Prices = Enumerable.Range(1, 5).Select(n => new DailyPricePrediction
                    {
                        Date = new DateOnly(2024, 3, 10).AddDays(n),
                        ClosingPrice = 100m + n * 10m,
                        HighPrice = 110m + n * 10m,
                        LowPrice = 90m + n * 10m
                    }).ToList()
                }
            });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().ContainSingle(a =>
            a.Symbol == new TradingSymbol("AMZN") &&
            a.Quantity == 1m &&
            a.OrderType == OrderType.LimitSell &&
            a.Price == 110m
        ).And.HaveCount(1);
    }

    [Fact]
    public async Task ShouldReturnCorrectActionInComplexCase()
    {
        _assets.GetCurrentAssetsAsync(Arg.Any<CancellationToken>()).Returns(new Assets
        {
            EquityValue = 600m,
            Cash = new Cash
            {
                MainCurrency = "USD",
                BuyingPower = 100m,
                AvailableAmount = 100m
            },
            Positions = new Dictionary<TradingSymbol, Position>()
        });

        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("AMZN"), Arg.Any<CancellationToken>())
            .Returns(100m);
        _marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TSLA"), Arg.Any<CancellationToken>())
            .Returns(200m);

        _predictor.GetPredictionsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<TradingSymbol, Prediction>
            {
                [new TradingSymbol("AMZN")] = new()
                {
                    Prices = new[]
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 11),
                            ClosingPrice = 100m,
                            HighPrice = 110m,
                            LowPrice = 100m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 12),
                            ClosingPrice = 100m,
                            HighPrice = 110m,
                            LowPrice = 90m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 13),
                            ClosingPrice = 200m,
                            HighPrice = 210m,
                            LowPrice = 190m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 14),
                            ClosingPrice = 200m,
                            HighPrice = 210m,
                            LowPrice = 190m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 15),
                            ClosingPrice = 200m,
                            HighPrice = 210m,
                            LowPrice = 190m
                        }
                    }
                },
                [new TradingSymbol("TSLA")] = new()
                {
                    Prices = new[]
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 11),
                            ClosingPrice = 200m,
                            HighPrice = 210m,
                            LowPrice = 190m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 12),
                            ClosingPrice = 250m,
                            HighPrice = 260m,
                            LowPrice = 240m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 13),
                            ClosingPrice = 250m,
                            HighPrice = 260m,
                            LowPrice = 240m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 14),
                            ClosingPrice = 250m,
                            HighPrice = 260m,
                            LowPrice = 240m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2024, 3, 15),
                            ClosingPrice = 250m,
                            HighPrice = 260m,
                            LowPrice = 240m
                        }
                    }
                }
            });

        var actions = await _strategy.GetTradingActionsAsync();

        actions.Should().BeEmpty();
    }
}
