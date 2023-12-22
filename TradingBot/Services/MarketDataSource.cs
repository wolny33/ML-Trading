﻿using Alpaca.Markets;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IMarketDataSource
{
    Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default);

    Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, CancellationToken token = default);

    Task<decimal> GetLastAvailablePriceForSymbolAsync(TradingSymbol symbol, CancellationToken token = default);
}

public sealed class MarketDataSource : IMarketDataSource
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IMarketDataCache _cache;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ILogger _logger;

    public MarketDataSource(IAlpacaClientFactory clientFactory, IAssetsDataSource assetsDataSource, ILogger logger,
        IMarketDataCache cache, IAlpacaCallQueue callQueue)
    {
        _clientFactory = clientFactory;
        _assetsDataSource = assetsDataSource;
        _cache = cache;
        _callQueue = callQueue;
        _logger = logger.ForContext<MarketDataSource>();
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        _logger.Debug("Getting prices from {Start} to {End}", start, end);

        var valid = await GetValidSymbolsAsync(token);
        var interestingValidSymbols = (await SendInterestingSymbolsRequestsAsync(token)).Where(s => valid.Contains(s));

        return await interestingValidSymbols.Chunk(15)
            .ToAsyncEnumerable()
            .SelectManyAwait(async chunk =>
                (await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, token)))).ToAsyncEnumerable())
            .Where(pair => IsDataValid(pair.TradingData))
            .Take(15)
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    public async Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol,
        DateOnly start, DateOnly end, CancellationToken token = default)
    {
        var data = await GetSymbolDataAsync(symbol, start, end, token);
        return IsDataValid(data.TradingData) ? data.TradingData : null;
    }

    public async Task<decimal> GetLastAvailablePriceForSymbolAsync(TradingSymbol symbol,
        CancellationToken token = default)
    {
        using var client = await _clientFactory.CreateMarketDataClientAsync(token);
        var latestTradeData = await _callQueue.SendRequestWithRetriesAsync(() => client
            .GetLatestTradeAsync(new LatestMarketDataRequest(symbol.Value), token)
            .ExecuteWithErrorHandling(_logger));
        return latestTradeData.Price;
    }

    private async Task<ISet<TradingSymbol>> GetValidSymbolsAsync(CancellationToken token = default)
    {
        if (_cache.TryGetValidSymbols() is { } cached)
        {
            _logger.Debug("Retrieved {Count} valid trading symbols from cache", cached.Count);
            return cached;
        }

        var validSymbols = await SendValidSymbolsRequestAsync(token);
        _cache.CacheValidSymbols(validSymbols.ToList());

        _logger.Debug("Retrieved {Count} valid trading symbols from Alpaca", validSymbols.Count);
        return validSymbols;
    }

    private async Task<ISet<TradingSymbol>> SendValidSymbolsRequestAsync(CancellationToken token = default)
    {
        using var tradingClient = await _clientFactory.CreateTradingClientAsync(token);
        var assetsRequest = new AssetsRequest
        {
            AssetClass = AssetClass.UsEquity,
            AssetStatus = AssetStatus.Active
        };
        var availableAssets = await _callQueue.SendRequestWithRetriesAsync(() =>
            tradingClient.ListAssetsAsync(assetsRequest, token).ExecuteWithErrorHandling(_logger), _logger);
        return availableAssets.Where(a => a is { Fractionable: true, IsTradable: true })
            .Select(a => new TradingSymbol(a.Symbol)).ToHashSet();
    }

    private async Task<IEnumerable<TradingSymbol>> SendInterestingSymbolsRequestsAsync(
        CancellationToken token = default)
    {
        const int maxRequestSize = 100;
        using var dataClient = await _clientFactory.CreateMarketDataClientAsync(token);

        var held = (await _assetsDataSource.GetCurrentAssetsAsync(token)).Positions.Keys.ToList();
        _logger.Debug("Retrieved held tokens: {Tokens}", held.Select(t => t.Value).ToList());
        var active = (await _callQueue.SendRequestWithRetriesAsync(() => dataClient
                .ListMostActiveStocksByVolumeAsync(maxRequestSize, token)
                .ExecuteWithErrorHandling(_logger), _logger))
            .Select(a => new TradingSymbol(a.Symbol)).ToList();
        _logger.Debug("Retrieved most active tokens: {Active}", active);

        return held.Concat(active).Distinct();
    }

    private async Task<TradingSymbolData> GetSymbolDataAsync(TradingSymbol symbol, DateOnly start, DateOnly end,
        CancellationToken token = default)
    {
        if (_cache.TryGetCachedData(symbol, start, end) is { } cached)
        {
            _logger.Verbose("Retrieved {Token} data between {Start} and {End} from cache", symbol.Value, start, end);
            return new TradingSymbolData(symbol, cached);
        }

        var bars = await SendBarsRequestAsync(symbol, start, end, token);
        _cache.CacheDailySymbolData(symbol, bars, start, end);
        _logger.Verbose("Retrieved {Token} data between {Start} and {End} from Alpaca", symbol.Value, start, end);
        return new TradingSymbolData(symbol, bars);
    }

    private async Task<IReadOnlyList<DailyTradingData>> SendBarsRequestAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        using var client = await _clientFactory.CreateMarketDataClientAsync(token);

        var startTime = start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var endTime = end.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var interval = new Interval<DateTime>(startTime, endTime);
        var barTimeFrame = new BarTimeFrame(1, BarTimeFrameUnit.Day);

        _logger.Verbose("Sending bars request for token {Token} in interval {Start} to {End}", symbol.Value, start,
            end);
        var bars = await _callQueue.SendRequestWithRetriesAsync(() => client
            .ListHistoricalBarsAsync(new HistoricalBarsRequest(symbol.Value, barTimeFrame, interval), token)
            .ExecuteWithErrorHandling(_logger));

        return bars.Items.Select(b => new DailyTradingData
        {
            Date = DateOnly.FromDateTime(b.TimeUtc),
            Open = b.Open,
            Close = b.Close,
            High = b.High,
            Low = b.Low,
            Volume = b.Volume
        }).ToList();
    }

    private bool IsDataValid(IReadOnlyList<DailyTradingData> dailyData)
    {
        bool HasNonPositiveValues(DailyTradingData data)
        {
            return data.Open <= 0 || data.Close <= 0 || data.High <= 0 || data.Low <= 0 || data.Volume <= 0;
        }

        bool HasInvalidHighAndLowPrices(DailyTradingData data)
        {
            return data.Low > data.Open || data.Low > data.Close || data.High < data.Open || data.High < data.Close;
        }

        if (!dailyData.Any())
        {
            _logger.Verbose("Market data is empty");
            return false;
        }

        foreach (var d in dailyData)
        {
            if (HasNonPositiveValues(d))
            {
                _logger.Warning("Market data entry is invalid - entry contains non-positive values: {Entry}", d);
                return false;
            }

            if (HasInvalidHighAndLowPrices(d))
            {
                _logger.Warning("Market data entry is invalid - high/low prices are not correct: {Entry}", d);
                return false;
            }
        }

        return true;
    }

    private sealed record TradingSymbolData(TradingSymbol Symbol, IReadOnlyList<DailyTradingData> TradingData);
}
