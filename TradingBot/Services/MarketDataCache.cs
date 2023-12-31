﻿using Microsoft.Extensions.Caching.Memory;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IMarketDataCache
{
    IReadOnlyList<DailyTradingData>? TryGetCachedData(TradingSymbol symbol, DateOnly start, DateOnly end);

    ISet<TradingSymbol>? TryGetValidSymbols();

    IEnumerable<TradingSymbol> GetMostActiveCachedSymbolsForDay(DateOnly day);

    decimal? GetLastCachedPrice(TradingSymbol symbol, DateOnly day);

    void CacheDailySymbolData(TradingSymbol symbol, IReadOnlyList<DailyTradingData> data, DateOnly start,
        DateOnly end);

    void CacheValidSymbols(IReadOnlyList<TradingSymbol> symbols);

    MemoryCacheStatistics GetCacheStats();
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

    public decimal? GetLastCachedPrice(TradingSymbol symbol, DateOnly day)
    {
        DailyTradingData? data;
        do
        {
            if (!_cache.TryGetValue<DailyTradingData?>(new CacheKey(symbol, day), out data)) return null;

            day = day.AddDays(-1);
        } while (data is null);

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
        _validSymbols = symbols.ToHashSet();
    }

    public MemoryCacheStatistics GetCacheStats()
    {
        return _cache.GetCurrentStatistics() ?? throw new InvalidOperationException("Cache stats are not tracked");
    }

    internal sealed record CacheKey(TradingSymbol Symbol, DateOnly Date);

    private sealed record SymbolWithData(TradingSymbol Symbol, DailyTradingData? Data);
}
