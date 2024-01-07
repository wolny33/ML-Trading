using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBotTests;

[Trait("Category", "Unit")]
public sealed class MarketDataCacheTests
{
    private readonly MarketDataCache _marketDataCache;
    private readonly IMemoryCache _memoryCache;
    private readonly DateOnly _today = new(2023, 12, 19);

    public MarketDataCacheTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _marketDataCache = new MarketDataCache(_memoryCache);
    }

    [Fact]
    public void ShouldReturnNullIfThereIsNoDataCached()
    {
        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN1"), _today, _today).Should().BeNull();
    }

    [Fact]
    public void ShouldReturnNullIfNotAllDaysArePresent()
    {
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today.AddDays(-1)),
            new DailyTradingData
            {
                Date = _today.AddDays(-1),
                Open = 2m,
                Close = 3m,
                High = 4m,
                Low = 1m,
                Volume = 10m
            });

        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN1"), _today.AddDays(-1), _today).Should().BeNull();
    }

    [Fact]
    public void ShouldReturnCachedValuesIfAllDaysArePresent()
    {
        var yesterday = new DailyTradingData
        {
            Date = _today.AddDays(-1),
            Open = 2m,
            Close = 3m,
            High = 4m,
            Low = 1m,
            Volume = 10m
        };
        var today = new DailyTradingData
        {
            Date = _today,
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), yesterday.Date), yesterday);
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), today.Date), today);

        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN1"), _today.AddDays(-1), _today).Should()
            .BeEquivalentTo(new[] { yesterday, today }, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ShouldReturnCachedValuesIfSomeDaysArePresentButNull()
    {
        var yesterday = new DailyTradingData
        {
            Date = _today.AddDays(-1),
            Open = 2m,
            Close = 3m,
            High = 4m,
            Low = 1m,
            Volume = 10m
        };
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), yesterday.Date), yesterday);
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today), (DailyTradingData?)null);

        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN1"), _today.AddDays(-1), _today).Should()
            .BeEquivalentTo(new[] { yesterday });
    }

    [Fact]
    public void ShouldCorrectlyCacheData()
    {
        var yesterday = new DailyTradingData
        {
            Date = _today.AddDays(-1),
            Open = 2m,
            Close = 3m,
            High = 4m,
            Low = 1m,
            Volume = 10m
        };
        var today = new DailyTradingData
        {
            Date = _today,
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };

        _marketDataCache.CacheDailySymbolData(new TradingSymbol("TKN1"), new[] { yesterday, today }, yesterday.Date,
            today.Date);

        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), yesterday.Date),
                out var cachedYesterday).Should().BeTrue();
        cachedYesterday.Should().BeEquivalentTo(yesterday);
        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), today.Date),
                out var cachedToday).Should().BeTrue();
        cachedToday.Should().BeEquivalentTo(today);
    }

    [Fact]
    public void ShouldCorrectlyCacheDataWithMissingDays()
    {
        var threeDaysAgo = new DailyTradingData
        {
            Date = _today.AddDays(-3),
            Open = 2m,
            Close = 3m,
            High = 4m,
            Low = 1m,
            Volume = 10m
        };
        var yesterday = new DailyTradingData
        {
            Date = _today.AddDays(-1),
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };

        _marketDataCache.CacheDailySymbolData(new TradingSymbol("TKN1"), new[] { threeDaysAgo, yesterday },
            _today.AddDays(-4), _today);

        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today.AddDays(-4)),
                out var cachedFourDaysAgo).Should().BeTrue();
        cachedFourDaysAgo.Should().BeNull();
        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today.AddDays(-3)),
                out var cachedThreeDaysAgo).Should().BeTrue();
        cachedThreeDaysAgo.Should().BeEquivalentTo(threeDaysAgo);
        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today.AddDays(-2)),
                out var cachedTwoDaysAgo).Should().BeTrue();
        cachedTwoDaysAgo.Should().BeNull();
        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today.AddDays(-1)),
                out var cachedYesterday).Should().BeTrue();
        cachedYesterday.Should().BeEquivalentTo(yesterday);
        _memoryCache
            .TryGetValue<DailyTradingData?>(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today),
                out var cachedToday).Should().BeTrue();
        cachedToday.Should().BeNull();
    }

    [Fact]
    public void ShouldReturnPreviouslyCachedData()
    {
        var yesterday = new DailyTradingData
        {
            Date = _today.AddDays(-1),
            Open = 2m,
            Close = 3m,
            High = 4m,
            Low = 1m,
            Volume = 10m
        };
        var today = new DailyTradingData
        {
            Date = _today,
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };

        _marketDataCache.CacheDailySymbolData(new TradingSymbol("TKN1"), new[] { yesterday, today }, _today.AddDays(-2),
            today.Date);

        _marketDataCache.TryGetCachedData(new TradingSymbol("TKN1"), _today.AddDays(-2), _today.AddDays(-1)).Should()
            .BeEquivalentTo(new[] { yesterday });
    }

    [Fact]
    public void ShouldCorrectlyReturnMostActiveCachedSymbols()
    {
        var firstTokenData = new DailyTradingData
        {
            Date = _today,
            Open = 2m,
            Close = 3m,
            High = 4m,
            Low = 1m,
            Volume = 10m
        };
        var secondTokenData = new DailyTradingData
        {
            Date = _today,
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today), firstTokenData);
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN2"), _today), secondTokenData);
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN3"), _today), (DailyTradingData?)null);

        _marketDataCache.CacheValidSymbols(new[] { "TKN1", "TKN2", "TKN3", "TKN4" }.Select(s => new TradingSymbol(s))
            .ToList());

        _marketDataCache.GetMostActiveCachedSymbolsForDay(_today).Should()
            .BeEquivalentTo(new[] { new TradingSymbol("TKN2"), new TradingSymbol("TKN1") },
                options => options.WithStrictOrdering());
    }

    [Fact]
    public void ShouldCorrectlyReturnLastCachedPriceForSymbol()
    {
        var today = new DailyTradingData
        {
            Date = _today,
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), today.Date), today);

        _marketDataCache.GetLastCachedPrice(new TradingSymbol("TKN1"), today.Date).Should().Be(4m);
    }

    [Fact]
    public void ShouldCorrectlyReturnLastCachedPriceForSymbolIfMostRecentDayIsNull()
    {
        var yesterday = new DailyTradingData
        {
            Date = _today.AddDays(-1),
            Open = 3m,
            Close = 4m,
            High = 5m,
            Low = 2m,
            Volume = 11m
        };
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), yesterday.Date), yesterday);
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today), (DailyTradingData?)null);

        _marketDataCache.GetLastCachedPrice(new TradingSymbol("TKN1"), _today).Should().Be(4m);
    }

    [Fact]
    public void ShouldReturnNullLastPriceIfThereIsNoCachedData()
    {
        _marketDataCache.GetLastCachedPrice(new TradingSymbol("TKN1"), _today).Should().BeNull();
    }

    [Fact]
    public void ShouldReturnNullLastPriceIfAllDaysAreNull()
    {
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today.AddDays(-1)),
            (DailyTradingData?)null);
        _memoryCache.Set(new MarketDataCache.CacheKey(new TradingSymbol("TKN1"), _today), (DailyTradingData?)null);

        _marketDataCache.GetLastCachedPrice(new TradingSymbol("TKN1"), _today).Should().BeNull();
    }
}
