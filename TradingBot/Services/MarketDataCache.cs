using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IMarketDataCache
{
    IReadOnlyList<DailyTradingData>? TryGetCachedData(TradingSymbol symbol, DateOnly start, DateOnly end);

    IReadOnlyList<TradingSymbol>? TryGetValidSymbols();

    IEnumerable<TradingSymbol> GetMostActiveCachedSymbolsForLastValidDay(DateOnly day);
    IEnumerable<TradingSymbol> GetMostActiveCachedSymbolsForDay(DateOnly day);
    IReadOnlyDictionary<DateOnly, double>? TryGetFearGreedIndexes(DateOnly start, DateOnly end);

    decimal? GetLastCachedPrice(TradingSymbol symbol, DateOnly day);

    void CacheDailySymbolData(TradingSymbol symbol, IReadOnlyList<DailyTradingData> data, DateOnly start, DateOnly end);

    void CacheValidSymbols(IReadOnlyList<TradingSymbol> symbols);
    void CacheFearGreedIndexes(IReadOnlyDictionary<DateOnly, double> values);

    MemoryCacheStatistics GetCacheStats();
}

public sealed class MarketDataCache : IMarketDataCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<DateOnly, double> _fearGreedIndexes = new();
    private IReadOnlyList<TradingSymbol>? _validSymbols;

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

    public IReadOnlyList<TradingSymbol>? TryGetValidSymbols()
    {
        return _validSymbols;
    }

    public IEnumerable<TradingSymbol> GetMostActiveCachedSymbolsForLastValidDay(DateOnly day)
    {
        var currentDay = day;
        do
        {
            var symbols = GetMostActiveCachedSymbolsForDay(currentDay).ToList();
            if (symbols.Any()) return symbols;

            currentDay = currentDay.AddDays(-1);
        } while (currentDay > day.AddDays(-5));

        throw new InvalidOperationException("Data is missing from cache");
    }

    public IEnumerable<TradingSymbol> GetMostActiveCachedSymbolsForDay(DateOnly day)
    {
        return _validSymbols?
                   .Select(s => new SymbolWithData(
                       s, _cache.TryGetValue<DailyTradingData?>(new CacheKey(s, day), out var data) ? data : null
                   ))
                   .Where(d => d.Data is not null)
                   .OrderByDescending(d => d.Data!.Volume)
                   .Select(d => d.Symbol)
               ?? throw new InvalidOperationException("Valid symbols were not cached");
    }

    public IReadOnlyDictionary<DateOnly, double>? TryGetFearGreedIndexes(DateOnly start, DateOnly end)
    {
        for (var day = start; day <= end; day = day.AddDays(1))
            if (!_fearGreedIndexes.ContainsKey(day))
                return null;

        return _fearGreedIndexes.ToArray().ToImmutableDictionary();
    }

    public decimal? GetLastCachedPrice(TradingSymbol symbol, DateOnly day)
    {
        DailyTradingData? data;
        do
        {
            if (!_cache.TryGetValue<DailyTradingData?>(new CacheKey(symbol, day), out data)) return null;

            day = day.AddDays(-1);
        } while (data is null || data.Close <= 0 || data.Volume <= 0);

        return data.Close;
    }

    public void CacheDailySymbolData(TradingSymbol symbol, IReadOnlyList<DailyTradingData> data, DateOnly start,
        DateOnly end)
    {
        var dailyData = data.ToDictionary(d => d.Date);
        for (var day = start; day <= end; day = day.AddDays(1))
            _cache.Set(new CacheKey(symbol, day), dailyData.TryGetValue(day, out var value) ? value : null);
    }

    public void CacheValidSymbols(IReadOnlyList<TradingSymbol> symbols)
    {
        _validSymbols = symbols;
    }

    public void CacheFearGreedIndexes(IReadOnlyDictionary<DateOnly, double> values)
    {
        foreach (var (day, value) in values) _fearGreedIndexes[day] = value;
    }

    public MemoryCacheStatistics GetCacheStats()
    {
        return _cache.GetCurrentStatistics() ?? throw new InvalidOperationException("Cache stats are not tracked");
    }

    internal sealed record CacheKey(TradingSymbol Symbol, DateOnly Date);

    private sealed record SymbolWithData(TradingSymbol Symbol, DailyTradingData? Data);
}
