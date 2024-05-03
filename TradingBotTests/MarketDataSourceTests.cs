using Alpaca.Markets;
using FluentAssertions;
using NSubstitute;
using Serilog;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class MarketDataSourceTests : IAsyncDisposable
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IAlpacaDataClient _dataClient;
    private readonly IMarketDataCache _marketDataCache;
    private readonly MarketDataSource _marketDataSource;
    private readonly IAlpacaTradingClient _tradingClient;
    private readonly ICurrentTradingTask _tradingTask;
    private readonly IBacktestAssets _backtestAssets;

    public MarketDataSourceTests()
    {
        _tradingClient = Substitute.For<IAlpacaTradingClient>();
        _dataClient = Substitute.For<IAlpacaDataClient>();
        var clientFactory = Substitute.For<IAlpacaClientFactory>();
        clientFactory.CreateTradingClientAsync(Arg.Any<CancellationToken>()).Returns(_tradingClient);
        clientFactory.CreateMarketDataClientAsync(Arg.Any<CancellationToken>()).Returns(_dataClient);

        _marketDataCache = Substitute.For<IMarketDataCache>();
        _marketDataCache.TryGetFearGreedIndexes(Arg.Any<DateOnly>(), Arg.Any<DateOnly>()).ReturnsForAnyArgs(call =>
            MakeFearGreedDictionary(call.ArgAt<DateOnly>(0), call.ArgAt<DateOnly>(1)));

        _assetsDataSource = Substitute.For<IAssetsDataSource>();
        var logger = Substitute.For<ILogger>();
        var callQueue = new CallQueueMock();

        _tradingTask = Substitute.For<ICurrentTradingTask>();
        _tradingTask.CurrentBacktestId.Returns((Guid?)null);
        _tradingTask.SymbolSlice.Returns(new BacktestSymbolSlice(0, -1));

        _backtestAssets = Substitute.For<IBacktestAssets>();

        _marketDataSource = new MarketDataSource(clientFactory, _assetsDataSource, logger, _marketDataCache, callQueue,
            _tradingTask, new ExcludedBacktestSymbols(), _backtestAssets);
    }

    public ValueTask DisposeAsync()
    {
        return _marketDataSource.DisposeAsync();
    }

    private static IReadOnlyDictionary<DateOnly, double> MakeFearGreedDictionary(DateOnly start, DateOnly end)
    {
        var result = new Dictionary<DateOnly, double>();
        for (var day = start; day <= end; day = day.AddDays(1)) result[day] = 50;

        return result;
    }

    [Fact]
    public async Task ShouldCorrectlyReturnPrices()
    {
        SetUpResponses();

        var result = await _marketDataSource.GetPricesAsync(DateOnly.MinValue, new DateOnly(2010, 12, 19));

        result.Should().HaveCount(2);
        result.Should().ContainKey(new TradingSymbol("TKN1")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m,
                FearGreedIndex = 50m
            }
        });
        result.Should().ContainKey(new TradingSymbol("TKN2")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 12m,
                Close = 13m,
                High = 14m,
                Low = 11m,
                Volume = 100m,
                FearGreedIndex = 50m
            }
        });

        _marketDataCache.Received(1).CacheValidSymbols(Arg.Is<IReadOnlyList<TradingSymbol>>(symbols =>
            symbols.Contains(new TradingSymbol("TKN1")) && symbols.Contains(new TradingSymbol("TKN2")) &&
            symbols.Contains(new TradingSymbol("TKN4")) && symbols.Contains(new TradingSymbol("TKN5"))));

        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN1"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN2"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
    }

    [Fact]
    public async Task ShouldReturnDataFromCacheIfAvailable()
    {
        SetUpResponses();

        _marketDataCache.TryGetValidSymbols().Returns(new TradingSymbol[] { new("TKN2"), new("TKN3") });
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN2"), DateOnly.MinValue, new DateOnly(2010, 12, 19)).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m,
                FearGreedIndex = 50m
            }
        });
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN1"), DateOnly.MinValue, new DateOnly(2010, 12, 19)).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 12m,
                Close = 13m,
                High = 14m,
                Low = 11m,
                Volume = 100m,
                FearGreedIndex = 50m
            }
        });
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN3"), DateOnly.MinValue, new DateOnly(2010, 12, 19)).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 22m,
                Close = 23m,
                High = 24m,
                Low = 21m,
                Volume = 200m,
                FearGreedIndex = 50m
            }
        });
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN4"), DateOnly.MinValue, new DateOnly(2010, 12, 19)).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m,
                FearGreedIndex = 50m
            }
        });

        var result = await _marketDataSource.GetPricesAsync(DateOnly.MinValue, new DateOnly(2010, 12, 19));

        result.Should().HaveCount(1);
        result.Should().ContainKey(new TradingSymbol("TKN2")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m,
                FearGreedIndex = 50m
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
    public async Task ShouldGetMostActiveSymbolsFromCacheInBacktest()
    {
        var testGuid = Guid.NewGuid();
        _tradingTask.CurrentBacktestId.Returns(testGuid);
        _tradingTask.GetTaskDay().Returns(new DateOnly(2010, 12, 19));
        _backtestAssets.GetForBacktestWithId(testGuid).Returns(new Assets
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

        _marketDataCache.TryGetValidSymbols().Returns(new TradingSymbol[] { new("TKN2"), new("TKN4") });
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN4"), DateOnly.MinValue, new DateOnly(2010, 12, 19)).Returns(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m,
                FearGreedIndex = 68m
            }
        });
        _marketDataCache.GetMostActiveCachedSymbolsForLastValidDay(new DateOnly(2010, 12, 19))
            .Returns(new[] { new TradingSymbol("TKN4") });

        var result = await _marketDataSource.GetPricesAsync(DateOnly.MinValue, new DateOnly(2010, 12, 19));

        result.Should().HaveCount(1);
        result.Should().ContainKey(new TradingSymbol("TKN4")).WhoseValue.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 112m,
                Close = 113m,
                High = 114m,
                Low = 111m,
                Volume = 1000m,
                FearGreedIndex = 68m
            }
        });

        await _dataClient.DidNotReceive()
            .ListMostActiveStocksByVolumeAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _tradingClient.DidNotReceive().ListAssetsAsync(Arg.Any<AssetsRequest>(), Arg.Any<CancellationToken>());
        await _dataClient.DidNotReceive()
            .ListHistoricalBarsAsync(Arg.Any<HistoricalBarsRequest>(), Arg.Any<CancellationToken>());

        _marketDataCache.DidNotReceive().CacheValidSymbols(Arg.Any<IReadOnlyList<TradingSymbol>>());
        _marketDataCache.DidNotReceive().CacheDailySymbolData(new TradingSymbol("TKN4"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>());
    }

    [Fact]
    public async Task ShouldCorrectlyReturnPricesForSingleSymbol()
    {
        SetUpResponses();

        var result =
            await _marketDataSource.GetDataForSingleSymbolAsync(new TradingSymbol("TKN3"), DateOnly.MinValue,
                new DateOnly(2010, 12, 19));

        result.Should().BeEquivalentTo(new[]
        {
            new DailyTradingData
            {
                Date = new DateOnly(2010, 12, 19),
                Open = 22m,
                Close = 23m,
                High = 24m,
                Low = 21m,
                Volume = 200m,
                FearGreedIndex = 50m
            }
        });

        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN3"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
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

    [Fact]
    public async Task ShouldReturnClosePriceFromCacheInBacktest()
    {
        _tradingTask.CurrentBacktestId.Returns(Guid.NewGuid());
        _tradingTask.GetTaskDay().Returns(new DateOnly(2010, 12, 19));

        _marketDataCache.GetLastCachedPrice(new TradingSymbol("TKN6"), new DateOnly(2010, 12, 19)).Returns(12m);

        (await _marketDataSource.GetLastAvailablePriceForSymbolAsync(new TradingSymbol("TKN6"))).Should().Be(12m);

        await _dataClient.DidNotReceive()
            .GetLatestTradeAsync(Arg.Any<LatestMarketDataRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldCorrectlyInitializeCacheForBacktest()
    {
        SetUpResponses();

        await _marketDataSource.InitializeBacktestDataAsync(DateOnly.MinValue, new DateOnly(2010, 12, 19),
            new BacktestSymbolSlice(0, -1), Guid.NewGuid());

        _marketDataCache.Received(1).CacheValidSymbols(Arg.Is<IReadOnlyList<TradingSymbol>>(symbols =>
            symbols.Contains(new TradingSymbol("TKN1")) && symbols.Contains(new TradingSymbol("TKN2")) &&
            symbols.Contains(new TradingSymbol("TKN4")) && symbols.Contains(new TradingSymbol("TKN5"))));

        await _dataClient.Received(1)
            .ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN1"),
                Arg.Any<CancellationToken>());
        await _dataClient.Received(1)
            .ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN2"),
                Arg.Any<CancellationToken>());
        await _dataClient.Received(1)
            .ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN4"),
                Arg.Any<CancellationToken>());
        await _dataClient.Received(1)
            .ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN5"),
                Arg.Any<CancellationToken>());

        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN1"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN2"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN4"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
        _marketDataCache.Received(1).CacheDailySymbolData(new TradingSymbol("TKN5"),
            Arg.Any<IReadOnlyList<DailyTradingData>>(), DateOnly.MinValue, new DateOnly(2010, 12, 19));
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
        // Active, valid symbol but invalid data
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
        tkn1Bar.TimeUtc.Returns(new DateTime(2010, 12, 19, 22, 41, 0));
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
        tkn2Bar.TimeUtc.Returns(new DateTime(2010, 12, 19, 22, 41, 0));
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
        tkn3Bar.TimeUtc.Returns(new DateTime(2010, 12, 19, 22, 41, 0));
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

        var tkn4Bar = Substitute.For<IBar>();
        tkn4Bar.TimeUtc.Returns(new DateTime(2010, 12, 19, 22, 41, 0));
        tkn4Bar.Open.Returns(32m);
        tkn4Bar.Close.Returns(33m);
        tkn4Bar.High.Returns(34m);
        tkn4Bar.Low.Returns(31m);
        tkn4Bar.Volume.Returns(300m);
        var tkn4Page = Substitute.For<IPage<IBar>>();
        tkn4Page.Items.Returns(new[] { tkn4Bar });
        tkn4Page.NextPageToken.Returns((string?)null);
        _dataClient.ListHistoricalBarsAsync(Arg.Is<HistoricalBarsRequest>(r => r.Symbols.Single() == "TKN4"))
            .Returns(tkn4Page);

        var tkn5Bar = Substitute.For<IBar>();
        tkn5Bar.TimeUtc.Returns(new DateTime(2010, 12, 19, 22, 41, 0));
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

        _marketDataCache.TryGetValidSymbols().Returns((IReadOnlyList<TradingSymbol>?)null);
        _marketDataCache.TryGetCachedData(Arg.Any<TradingSymbol>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>())
            .Returns((IReadOnlyList<DailyTradingData>?)null);
    }
}
