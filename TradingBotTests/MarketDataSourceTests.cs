using Alpaca.Markets;
using FluentAssertions;
using NSubstitute;
using Serilog;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class MarketDataSourceTests
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IAlpacaDataClient _dataClient;
    private readonly IMarketDataCache _marketDataCache;
    private readonly MarketDataSource _marketDataSource;
    private readonly IAlpacaTradingClient _tradingClient;

    public MarketDataSourceTests()
    {
        _tradingClient = Substitute.For<IAlpacaTradingClient>();
        _dataClient = Substitute.For<IAlpacaDataClient>();
        var clientFactory = Substitute.For<IAlpacaClientFactory>();
        clientFactory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClient);
        clientFactory.CreateMarketDataClientAsync(Arg.Any<CancellationToken>()).Returns(_dataClient);

        _marketDataCache = Substitute.For<IMarketDataCache>();
        _assetsDataSource = Substitute.For<IAssetsDataSource>();
        var logger = Substitute.For<ILogger>();
        var callQueue = new CallQueueMock();

        var tradingTask = Substitute.For<ICurrentTradingTask>();
        tradingTask.CurrentBacktestId.Returns((Guid?)null);

        _marketDataSource = new MarketDataSource(clientFactory, _assetsDataSource, logger, _marketDataCache, callQueue,
            tradingTask);
    }

    [Fact]
    public async Task ShouldCorrectlyReturnPrices()
    {
        SetUpResponses();

        var result = await _marketDataSource.GetPricesAsync(DateOnly.MinValue, DateOnly.MaxValue);

        result.Should().HaveCount(2);
        result.Should().ContainKey(new TradingSymbol("TKN1")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2023, 12, 19),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            }
        });
        result.Should().ContainKey(new TradingSymbol("TKN2")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2023, 12, 19),
                Open = 12m,
                Close = 13m,
                High = 14m,
                Low = 11m,
                Volume = 100m
            }
        });

        _marketDataCache.Received(1).CacheValidSymbols(Arg.Is<IReadOnlyList<TradingSymbol>>(symbols =>
            symbols.Contains(new TradingSymbol("TKN1")) && symbols.Contains(new TradingSymbol("TKN2")) &&
            symbols.Contains(new TradingSymbol("TKN4")) && symbols.Contains(new TradingSymbol("TKN5"))));

        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN1"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, DateOnly.MaxValue);
        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN2"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, DateOnly.MaxValue);
    }

    [Fact]
    public async Task ShouldReturnDataFromCacheIfAvailable()
    {
        SetUpResponses();

        _marketDataCache.TryGetValidSymbols().Returns(new HashSet<TradingSymbol> { new("TKN2"), new("TKN3") });
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN2"), DateOnly.MinValue, DateOnly.MaxValue).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2023, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m
            }
        });

        var result = await _marketDataSource.GetPricesAsync(DateOnly.MinValue, DateOnly.MaxValue);

        result.Should().HaveCount(1);
        result.Should().ContainKey(new TradingSymbol("TKN2")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2023, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m
            }
        });

        await _tradingClient.DidNotReceive().ListAssetsAsync(Arg.Any<AssetsRequest>(), Arg.Any<CancellationToken>());
        await _dataClient.DidNotReceive()
            .ListHistoricalBarsAsync(Arg.Any<HistoricalBarsRequest>(), Arg.Any<CancellationToken>());

        _marketDataCache.DidNotReceive().CacheValidSymbols(Arg.Any<IReadOnlyList<TradingSymbol>>());
        _marketDataCache.DidNotReceive().CacheDailySymbolData(new TradingSymbol("TKN2"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task ShouldCorrectlyReturnPricesForSingleSymbol()
    {
        SetUpResponses();

        var result =
            await _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN3"), DateOnly.MinValue,
                DateOnly.MaxValue);

        result.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2023, 12, 19),
                Open = 22m,
                Close = 23m,
                High = 24m,
                Low = 21m,
                Volume = 200m
            }
        });

        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN3"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, DateOnly.MaxValue);
    }

    [Fact]
    public async Task ShouldCorrectlyReturnLastPriceForSymbol()
    {
        var lastTrade = Substitute.For<ITrade>();
        lastTrade.Price.Returns(10m);
        _dataClient.GetLatestTradeAsync(Arg.Is<LatestMarketDataRequest>(r => r.Symbol == "TKN6"),
            Arg.Any<CancellationToken>()).Returns(lastTrade);

        var result = await _marketDataSource.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN6"));

        result.Should().Be(10m);
    }

    private void SetUpResponses()
    {
        var assetsResponse = new[]
        {
            Substitute.For<IAsset>(),
            Substitute.For<IAsset>(),
            Substitute.For<IAsset>(),
            Substitute.For<IAsset>(),
            Substitute.For<IAsset>()
        };
        // Valid, most active
        assetsResponse[0].Symbol.Returns("TKN1");
        assetsResponse[0].Fractionable.Returns(true);
        assetsResponse[0].IsTradable.Returns(true);
        // Valid, held
        assetsResponse[1].Symbol.Returns("TKN2");
        assetsResponse[1].Fractionable.Returns(true);
        assetsResponse[1].IsTradable.Returns(true);
        // Not valid
        assetsResponse[2].Symbol.Returns("TKN3");
        assetsResponse[2].Fractionable.Returns(true);
        assetsResponse[2].IsTradable.Returns(false);
        // Valid, not held or active
        assetsResponse[3].Symbol.Returns("TKN4");
        assetsResponse[3].Fractionable.Returns(true);
        assetsResponse[3].IsTradable.Returns(true);
        // Valid symbol but invalid data
        assetsResponse[4].Symbol.Returns("TKN5");
        assetsResponse[4].Fractionable.Returns(true);
        assetsResponse[4].IsTradable.Returns(true);

        _tradingClient.ListAssetsAsync(Arg.Any<AssetsRequest>(), Arg.Any<CancellationToken>()).Returns(assetsResponse);

        var activeStocksResponse = new[] { Substitute.For<IActiveStock>(), Substitute.For<IActiveStock>() };
        activeStocksResponse[0].Symbol.Returns("TKN1");
        activeStocksResponse[1].Symbol.Returns("TKN5");

        _dataClient.ListMostActiveStocksByVolumeAsync(100, Arg.Any<CancellationToken>()).Returns(activeStocksResponse);

        _assetsDataSource.GetCurrentAssetsAsync().Returns(new Assets
        {
            Cash = new Cash
            {
                AvailableAmount = 100m,
                BuyingPower = 100m,
                MainCurrency = "USD"
            },
            EquityValue = 100m,
            Positions = new Dictionary<TradingSymbol, Position>
            {
                [new TradingSymbol("TKN2")] = new()
                {
                    Symbol = new TradingSymbol("TKN2"),
                    Quantity = 1m,
                    AvailableQuantity = 1m,
                    AverageEntryPrice = 1m,
                    MarketValue = 1m,
                    SymbolId = Guid.NewGuid()
                }
            }
        });

        var tkn1Bar = Substitute.For<IBar>();
        tkn1Bar.TimeUtc.Returns(new DateTime(2023, 12, 19, 22, 41, 0));
        tkn1Bar.Open.Returns(2m);
        tkn1Bar.Close.Returns(3m);
        tkn1Bar.High.Returns(4m);
        tkn1Bar.Low.Returns(1m);
        tkn1Bar.Volume.Returns(10m);
        var tkn1Page = Substitute.For<IPage<IBar>>();
        tkn1Page.Items.Returns(new[] { tkn1Bar });
        tkn1Page.NextPageToken.Returns((string?)null);
        _dataClient.ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN1"))
            .Returns(tkn1Page);

        var tkn2Bar = Substitute.For<IBar>();
        tkn2Bar.TimeUtc.Returns(new DateTime(2023, 12, 19, 22, 41, 0));
        tkn2Bar.Open.Returns(12m);
        tkn2Bar.Close.Returns(13m);
        tkn2Bar.High.Returns(14m);
        tkn2Bar.Low.Returns(11m);
        tkn2Bar.Volume.Returns(100m);
        var tkn2Page = Substitute.For<IPage<IBar>>();
        tkn2Page.Items.Returns(new[] { tkn2Bar });
        tkn2Page.NextPageToken.Returns((string?)null);
        _dataClient.ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN2"))
            .Returns(tkn2Page);

        var tkn3Bar = Substitute.For<IBar>();
        tkn3Bar.TimeUtc.Returns(new DateTime(2023, 12, 19, 22, 41, 0));
        tkn3Bar.Open.Returns(22m);
        tkn3Bar.Close.Returns(23m);
        tkn3Bar.High.Returns(24m);
        tkn3Bar.Low.Returns(21m);
        tkn3Bar.Volume.Returns(200m);
        var tkn3Page = Substitute.For<IPage<IBar>>();
        tkn3Page.Items.Returns(new[] { tkn3Bar });
        tkn3Page.NextPageToken.Returns((string?)null);
        _dataClient.ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN3"))
            .Returns(tkn3Page);

        var tkn5Bar = Substitute.For<IBar>();
        tkn5Bar.TimeUtc.Returns(new DateTime(2023, 12, 19, 22, 41, 0));
        tkn5Bar.Open.Returns(0m);
        tkn5Bar.Close.Returns(13m);
        tkn5Bar.High.Returns(14m);
        tkn5Bar.Low.Returns(11m);
        tkn5Bar.Volume.Returns(100m);
        var tkn5Page = Substitute.For<IPage<IBar>>();
        tkn5Page.Items.Returns(new[] { tkn5Bar });
        tkn5Page.NextPageToken.Returns((string?)null);
        _dataClient.ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN5"))
            .Returns(tkn5Page);

        _marketDataCache.TryGetValidSymbols().Returns((ISet<TradingSymbol>?)null);
        _marketDataCache.TryGetCachedData(Arg.Any<TradingSymbol>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>())
            .Returns((IReadOnlyList<DailyTradingData>?)null);
    }
}
