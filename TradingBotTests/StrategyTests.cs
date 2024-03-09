using Alpaca.Markets;
using FluentAssertions;
using NSubstitute;
using TradingBot.Models;
using TradingBot.Services;
using TradingBot.Configuration;
using TradingBot.Services.Strategy;

namespace TradingBotTests
{
    [Trait("Category", "Unit")]
    public sealed class StrategyTests
    {
        private readonly IDictionary<TradingSymbol, Prediction> _predictions;
        private readonly Assets _assets;
        private readonly StrategyParametersConfiguration _strategyParameters;
        public StrategyTests()
        {
            _predictions = new Dictionary<TradingSymbol, Prediction>
            {
                [new TradingSymbol("TSLA")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 238.405077066421508778432m,
                            HighPrice = 246.66130756638944149024464m,
                            LowPrice = 233.48703374415636063042m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 238.11419290368937064966644686m,
                            HighPrice = 246.40406112291012496019389897m,
                            LowPrice = 233.48073286238147814750750690m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 238.12185341992876140069643655m,
                            HighPrice = 246.14116784587022328943201235m,
                            LowPrice = 233.67585574587523818735511651m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 238.18518688600698779117844094m,
                            HighPrice = 245.77157939747751975504611287m,
                            LowPrice = 233.66958545947559976052541495m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 237.93334641874799449356894287m,
                            HighPrice = 245.27658628854233925619820412m,
                            LowPrice = 233.65393584347483400375150561m
                        }
                    }
                },
                [new TradingSymbol("MARA")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 15.21453737735748290944m,
                            HighPrice = 16.11181389715522528007m,
                            LowPrice = 14.94643731046468019027m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 15.146090751052113394769092583m,
                            HighPrice = 16.048618792943519382116672723m,
                            LowPrice = 14.911924286693271576293038894m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 15.109282148337061259257107954m,
                            HighPrice = 15.990700377599889361307735713m,
                            LowPrice = 14.897647005182265353476333162m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 15.077378700127566209560365751m,
                            HighPrice = 15.929167247235234054589735043m,
                            LowPrice = 14.876228805435325159079734649m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 15.032188710406313313534653017m,
                            HighPrice = 15.863840355948434055225398229m,
                            LowPrice = 14.853337789897704412990019794m
                        }
                    }
                },
                [new TradingSymbol("SOXS")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 8.84562028538435697771m,
                            HighPrice = 8.91851251438260078849m,
                            LowPrice = 8.678417233526706695480m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 8.903697919347435014042585462m,
                            HighPrice = 8.962905092105986281565648735m,
                            LowPrice = 8.755507511791212608209943330m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 8.968830116108417988813761690m,
                            HighPrice = 9.010568393820741947996685110m,
                            LowPrice = 8.845418448010368229598862418m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 9.047150805336194700975044031m,
                            HighPrice = 9.048751609047884491182432785m,
                            LowPrice = 8.919394635919414626896333669m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 9.081859470502282815185807552m,
                            HighPrice = 9.075213823844223153619516471m,
                            LowPrice = 8.990668198369737698531201804m
                        }
                    }
                },
                [new TradingSymbol("PLUG")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 4.26420837104320526104m,
                            HighPrice = 4.64825740277767181508m,
                            LowPrice = 4.15417857989668846092m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 4.2896781226174725387631285787m,
                            HighPrice = 4.6683309800042517384589963733m,
                            LowPrice = 4.1908390217179553262331446513m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 4.3196956549982462765548924506m,
                            HighPrice = 4.6892449490091341375174726442m,
                            LowPrice = 4.2327473719681794877023181043m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 4.3549842587443680793529526281m,
                            HighPrice = 4.7067070571748813011524673335m,
                            LowPrice = 4.2689696529518492280703033547m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 4.3701517653736764054418554314m,
                            HighPrice = 4.7178748572754473113198441748m,
                            LowPrice = 4.3023741744064084958687795963m
                        }
                    }
                }
            };
            _assets = new Assets
            {
                EquityValue = 12345m,
                Cash = new Cash
                {
                    AvailableAmount = 1555m,
                    MainCurrency = "EUR",
                    BuyingPower = 1234m
                },
                Positions = new Dictionary<TradingSymbol, Position>
                {
                    [new TradingSymbol("MARA")] = new Position
                    {
                        SymbolId = Guid.NewGuid(),
                        Symbol = new TradingSymbol("MARA"),
                        Quantity = 100m,
                        AvailableQuantity = 80m,
                        AverageEntryPrice = 234m,
                        MarketValue = 23456m
                    },
                    [new TradingSymbol("PLUG")] = new Position
                    {
                        SymbolId = Guid.NewGuid(),
                        Symbol = new TradingSymbol("PLUG"),
                        Quantity = 120m,
                        AvailableQuantity = 90m,
                        AverageEntryPrice = 9.81m,
                        MarketValue = 1357m
                    }
                }
            };
            _strategyParameters = new StrategyParametersConfiguration
            {
                MaxStocksBuyCount = 3,
                MinDaysDecreasing = 3,
                MinDaysIncreasing = 3,
                TopGrowingSymbolsBuyRatio = 0.4
            };
        }
        [Fact]
        public async Task ShouldCorrectlyGenerateTradeActions()
        {
            var tradingTask = Substitute.For<ICurrentTradingTask>();
            var time = DateTimeOffset.UtcNow;
            tradingTask.GetTaskTime().Returns(time);

            var predictior = Substitute.For<IPricePredictor>();
            predictior.GetPredictionsAsync().Returns(_predictions);

            var assetsData = Substitute.For<IAssetsDataSource>();
            assetsData.GetCurrentAssetsAsync().Returns(_assets);

            var PLUGCurrentPrice = 4.1417857989668846092m;
            var SOXSCurrentPrice = 8.84562028538435697771m;

            var marketData = Substitute.For<IMarketDataSource>();
            marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TSLA")).Returns(238.405077066421508778432m);
            marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("MARA")).Returns(15.21453737735748290944m);
            marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("SOXS")).Returns(SOXSCurrentPrice);
            marketData.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("PLUG")).Returns(PLUGCurrentPrice);

            var strategyParameters = Substitute.For<IStrategyParametersService>();
            strategyParameters.GetConfigurationAsync().Returns(_strategyParameters);

            var strategy = new Strategy(predictior, assetsData, marketData, strategyParameters, tradingTask);
            var tradeActions = await strategy.GetTradingActionsAsync();

            tradeActions.Should().ContainEquivalentOf(new TradingAction
            {
                Id = Guid.NewGuid(),
                CreatedAt = time,
                Price = null,
                Quantity = 80m,
                Symbol = new TradingSymbol("MARA"),
                InForce = TimeInForce.Day,
                OrderType = TradingBot.Models.OrderType.MarketSell
            }, options => options
                .Excluding(x => x.Id));

            var PLUGPrice = Math.Round((PLUGCurrentPrice + _predictions[new TradingSymbol("PLUG")].Prices[0].LowPrice) / 2, 2);
            var PLUGQuantity = (int)Math.Floor((_assets.Cash.BuyingPower * (decimal)_strategyParameters.TopGrowingSymbolsBuyRatio) / PLUGPrice);
            tradeActions.Should().ContainEquivalentOf(new TradingAction
            {
                Id = Guid.NewGuid(),
                CreatedAt = time,
                Price = PLUGPrice,
                Quantity = PLUGQuantity,
                Symbol = new TradingSymbol("PLUG"),
                InForce = TimeInForce.Day,
                OrderType = TradingBot.Models.OrderType.LimitBuy
            }, options => options
                .Excluding(x => x.Id));

            var SOXSPrice = Math.Round((SOXSCurrentPrice + _predictions[new TradingSymbol("SOXS")].Prices[0].LowPrice) / 2, 2);
            var SOXSQuantity = (int)((_assets.Cash.BuyingPower - PLUGQuantity * PLUGPrice) / SOXSPrice);
            tradeActions.Should().ContainEquivalentOf(new TradingAction
            {
                Id = Guid.NewGuid(),
                CreatedAt = time,
                Price = SOXSPrice,
                Quantity = SOXSQuantity,
                Symbol = new TradingSymbol("SOXS"),
                InForce = TimeInForce.Day,
                OrderType = TradingBot.Models.OrderType.LimitBuy
            }, options => options
                .Excluding(x => x.Id));
        }
    }
}
