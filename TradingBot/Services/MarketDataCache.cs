using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IMarketDataCache
{
    IReadOnlyList<DailyTradingData>? TryGetCachedData(TradingSymbol symbol, DateOnly start, DateOnly end);

    ISet<TradingSymbol>? TryGetValidSymbols();

    void CacheDailySymbolData(TradingSymbol symbol, IEnumerable<DailyTradingData> data, DateOnly start,
        DateOnly end);

    void CacheValidSymbols(IEnumerable<TradingSymbol> symbols);
}

public sealed class MarketDataCache : IMarketDataCache
{
    private readonly IMemoryCache _cache;
    private ISet<TradingSymbol>? _validSymbols;

    public MarketDataCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public IReadOnlyList<DailyTradingData>? TryGetCachedData(TradingSymbol symbol, DateOnly start, DateOnly end)
    {
        var result = new List<DailyTradingData>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (!_cache.TryGetValue<DailyTradingData?>(new CacheKey(symbol, day), out var value)) return null;

            if (value is not null) result.Add(value);
        }

        return result;
    }

    public ISet<TradingSymbol>? TryGetValidSymbols()
    {
        return _validSymbols;
    }

    public void CacheDailySymbolData(TradingSymbol symbol, IEnumerable<DailyTradingData> data, DateOnly start,
        DateOnly end)
    {
        var dailyData = data.ToDictionary(d => d.Date);
        for (var day = start; day <= end; day = day.AddDays(1))
            _cache.Set(new CacheKey(symbol, day), dailyData.TryGetValue(day, out var value) ? value : null);
    }

    public void CacheValidSymbols(IEnumerable<TradingSymbol> symbols)
    {
        _validSymbols = symbols.ToHashSet();
    }

    private sealed record CacheKey(TradingSymbol Symbol, DateOnly Date);
}
