﻿using Alpaca.Markets;
using TradingBot.Models;
using ILogger = Serilog.ILogger;

namespace TradingBot.Services;

public interface IMarketDataSource
{
    Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start, DateOnly end,
        CancellationToken token = default);

    Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesForAllSymbolsAsync(DateOnly start,
        DateOnly end, BacktestSymbolSlice? slice = null, CancellationToken token = default);

    Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, CancellationToken token = default);

    Task<decimal> GetLastAvailablePriceForSymbolAsync(TradingSymbol symbol, CancellationToken token = default);

    Task InitializeBacktestDataAsync(DateOnly start, DateOnly end, BacktestSymbolSlice slice, Guid backtestId,
        CancellationToken token = default);
}

public sealed class MarketDataSource : IMarketDataSource, IAsyncDisposable
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IBacktestAssets _backtestAssets;
    private readonly IMarketDataCache _cache;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly Lazy<Task<IAlpacaDataClient>> _dataClient;
    private readonly IExcludedBacktestSymbols _excludedSymbols;
    private readonly ILogger _logger;
    private readonly Lazy<Task<IAlpacaTradingClient>> _tradingClient;
    private readonly ICurrentTradingTask _tradingTask;

    public MarketDataSource(IAlpacaClientFactory clientFactory, IAssetsDataSource assetsDataSource, ILogger logger,
        IMarketDataCache cache, IAlpacaCallQueue callQueue, ICurrentTradingTask tradingTask,
        IExcludedBacktestSymbols excludedSymbols, IBacktestAssets backtestAssets)
    {
        _assetsDataSource = assetsDataSource;
        _cache = cache;
        _callQueue = callQueue;
        _tradingTask = tradingTask;
        _excludedSymbols = excludedSymbols;
        _backtestAssets = backtestAssets;
        _logger = logger.ForContext<MarketDataSource>();

        _dataClient = new Lazy<Task<IAlpacaDataClient>>(() => clientFactory.CreateMarketDataClientAsync());
        _tradingClient = new Lazy<Task<IAlpacaTradingClient>>(() => clientFactory.CreateTradingClientAsync());
    }

    public async ValueTask DisposeAsync()
    {
        if (_tradingClient.IsValueCreated) (await _tradingClient.Value).Dispose();

        if (_dataClient.IsValueCreated) (await _dataClient.Value).Dispose();
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        _logger.Debug("Getting prices for interesting and held symbols from {Start} to {End}", start, end);

        var valid = GetNotExcluded(await GetValidSymbolsAsync(_tradingTask.SymbolSlice, token));
        var interestingValidSymbols = (await GetInterestingSymbolsAsync(token)).Where(s => valid.Contains(s));

        return await interestingValidSymbols.Chunk(15)
            .ToAsyncEnumerable()
            .SelectManyAwait(async chunk =>
                (await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, token)))).ToAsyncEnumerable())
            .Where(pair => IsDataValid(pair.Symbol, pair.TradingData))
            .Take(100)
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesForAllSymbolsAsync(
        DateOnly start, DateOnly end, BacktestSymbolSlice? slice = null, CancellationToken token = default)
    {
        _logger.Debug("Getting prices for all symbols from {Start} to {End}", start, end);

        var valid = GetNotExcluded(await GetValidSymbolsAsync(slice ?? _tradingTask.SymbolSlice, token));
        return await valid.Chunk(50)
            .ToAsyncEnumerable()
            .SelectManyAwait(async chunk =>
            {
                _logger.Verbose("Getting data for chunk: {Symbols}", chunk.Select(t => t.Value).ToList());
                var dataForChunk = await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, token)));
                return dataForChunk.ToAsyncEnumerable();
            })
            .Where(pair => IsDataValid(pair.Symbol, pair.TradingData))
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    public async Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol,
        DateOnly start, DateOnly end, CancellationToken token = default)
    {
        var data = await GetSymbolDataAsync(symbol, start, end, token);
        return IsDataValid(symbol, data.TradingData) ? data.TradingData : null;
    }

    public async Task<decimal> GetLastAvailablePriceForSymbolAsync(TradingSymbol symbol,
        CancellationToken token = default)
    {
        if (_tradingTask.CurrentBacktestId is not null)
        {
            _logger.Verbose("Backtest is active - getting last price for {Symbol} from cache", symbol.Value);
            return _cache.GetLastCachedPrice(symbol, _tradingTask.GetTaskDay()) ??
                   throw new InvalidOperationException(
                       $"Last price for '{symbol.Value}' could not be retrieved from cache");
        }

        _logger.Verbose("Getting last price for {Symbol} from Alpaca", symbol.Value);
        return await SendLastTradeRequestAsync(symbol, token);
    }

    public async Task InitializeBacktestDataAsync(DateOnly start, DateOnly end, BacktestSymbolSlice slice,
        Guid backtestId, CancellationToken token = default)
    {
        _logger.Debug("Initializing cache for backtest (from {Start} to {End})", start, end);

        var valid = await GetValidSymbolsAsync(slice, token);
        var symbolData = await valid.Chunk(50).ToAsyncEnumerable().SelectManyAwait(async chunk =>
            {
                _logger.Verbose("Getting data for chunk: {Symbols}", chunk.Select(t => t.Value).ToList());
                var dataForChunk = await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, token)));
                return dataForChunk.ToAsyncEnumerable();
            })
            .ToListAsync(token);

        var excludedSymbols = symbolData.Where(data => HasSuddenPriceJumps(data.TradingData))
            .Select(data => data.Symbol).ToList();
        _logger.Debug("Symbols with sudden price jumps will be excluded from backtest: {Symbols}",
            excludedSymbols.Select(s => s.Value).ToList());
        _excludedSymbols.Set(backtestId, excludedSymbols);

        _logger.Debug("Cache was successfully initialized");
    }

    private async Task<decimal> SendLastTradeRequestAsync(TradingSymbol symbol, CancellationToken token)
    {
        var client = await _dataClient.Value;
        var latestTradeData = await _callQueue.SendRequestWithRetriesAsync(() => client
            .GetLatestTradeAsync(new LatestMarketDataRequest(symbol.Value), token)).ExecuteWithErrorHandling(_logger);
        return latestTradeData.Price;
    }

    private async Task<ISet<TradingSymbol>> GetValidSymbolsAsync(BacktestSymbolSlice slice,
        CancellationToken token = default)
    {
        if (_cache.TryGetValidSymbols() is { } cached)
        {
            _logger.Debug("Retrieved {Count} valid trading symbols from cache", cached.Count);
            var sliceFromCache = slice.Take == -1 ? cached.Skip(slice.Skip) : cached.Skip(slice.Skip).Take(slice.Take);
            return sliceFromCache.ToHashSet();
        }

        var validSymbols = await SendValidSymbolsRequestAsync(token);
        _cache.CacheValidSymbols(validSymbols);

        _logger.Debug("Retrieved {Count} valid trading symbols from Alpaca", validSymbols.Count);

        var validSymbolsSlice = slice.Take == -1
            ? validSymbols.Skip(slice.Skip)
            : validSymbols.Skip(slice.Skip).Take(slice.Take);
        return validSymbolsSlice.ToHashSet();
    }

    private IEnumerable<TradingSymbol> GetNotExcluded(IEnumerable<TradingSymbol> symbols)
    {
        if (_tradingTask.CurrentBacktestId is null)
        {
            return symbols;
        }

        var excluded = _excludedSymbols.Get(_tradingTask.CurrentBacktestId.Value);
        return symbols.Where(s => !excluded.Contains(s));
    }

    private async Task<IReadOnlyList<TradingSymbol>> SendValidSymbolsRequestAsync(CancellationToken token = default)
    {
        var tradingClient = await _tradingClient.Value;
        var assetsRequest = new AssetsRequest
        {
            AssetClass = AssetClass.UsEquity,
            AssetStatus = AssetStatus.Active
        };
        var availableAssets = await _callQueue.SendRequestWithRetriesAsync(() =>
            tradingClient.ListAssetsAsync(assetsRequest, token), _logger).ExecuteWithErrorHandling(_logger);
        return availableAssets.Where(a => a is { Fractionable: true, IsTradable: true })
            .Select(a => new TradingSymbol(a.Symbol)).OrderBy(s => s.Value).ToList();
    }

    private Task<IEnumerable<TradingSymbol>> GetInterestingSymbolsAsync(CancellationToken token = default)
    {
        return _tradingTask.CurrentBacktestId is not null
            ? Task.FromResult(_cache.GetMostActiveCachedSymbolsForLastValidDay(_tradingTask.GetTaskDay())
                .Concat(_backtestAssets.GetForBacktestWithId(_tradingTask.CurrentBacktestId.Value).Positions.Keys)
                .Distinct())
            : SendInterestingSymbolsRequestsAsync(token);
    }

    private async Task<IEnumerable<TradingSymbol>> SendInterestingSymbolsRequestsAsync(
        CancellationToken token = default)
    {
        const int maxRequestSize = 100;
        var dataClient = await _dataClient.Value;

        var held = (await _assetsDataSource.GetCurrentAssetsAsync(token)).Positions.Keys.ToList();
        _logger.Debug("Retrieved held tokens: {Tokens}", held.Select(t => t.Value).ToList());
        var active = (await _callQueue.SendRequestWithRetriesAsync(() => dataClient
                .ListMostActiveStocksByVolumeAsync(maxRequestSize, token), _logger).ExecuteWithErrorHandling(_logger))
            .Select(a => new TradingSymbol(a.Symbol)).ToList();
        _logger.Debug("Retrieved most active tokens: {Active}", active.Select(t => t.Value).ToList());

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
        var startTime = start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var endTime = end.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var interval = new Interval<DateTime>(startTime, endTime);
        var barTimeFrame = new BarTimeFrame(1, BarTimeFrameUnit.Day);

        _logger.Verbose("Sending bars request for token {Token} in interval {Start} to {End}", symbol.Value, start,
            end);

        return await GetAllPagesAsync().SelectMany(page => page.ToAsyncEnumerable()).ToListAsync(token);

        async IAsyncEnumerable<IReadOnlyList<DailyTradingData>> GetAllPagesAsync()
        {
            var client = await _dataClient.Value;

            string? nextPageToken = null;
            do
            {
                var request =
                    new HistoricalBarsRequest(symbol.Value, barTimeFrame, interval)
                        .WithPageSize(Pagination.MaxPageSize);
                if (nextPageToken is not null) request = request.WithPageToken(nextPageToken);

                var bars = await _callQueue
                    .SendRequestWithRetriesAsync(() => client.ListHistoricalBarsAsync(request, token))
                    .ExecuteWithErrorHandling(_logger);

                nextPageToken = bars.NextPageToken;
                yield return bars.Items.Select(b => new DailyTradingData
                {
                    Date = DateOnly.FromDateTime(b.TimeUtc),
                    Open = b.Open,
                    Close = b.Close,
                    High = b.High,
                    Low = b.Low,
                    Volume = b.Volume
                }).ToList();
            } while (nextPageToken is not null);
        }
    }

    private bool IsDataValid(TradingSymbol symbol, IReadOnlyList<DailyTradingData> dailyData)
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
            _logger.Verbose("Market data for {Symbol} is empty", symbol.Value);
            return false;
        }

        foreach (var d in dailyData)
        {
            if (HasNonPositiveValues(d))
            {
                _logger.Warning("Market data entry for {Symbol} is invalid - entry contains non-positive values",
                    symbol.Value);
                return false;
            }

            if (HasInvalidHighAndLowPrices(d))
            {
                _logger.Warning("Market data entry for {Symbol} is invalid - high/low prices are not correct",
                    symbol.Value);
                return false;
            }
        }

        return true;
    }

    private static bool HasSuddenPriceJumps(IReadOnlyList<DailyTradingData> data)
    {
        var prices = data.Select(d => d.Close).ToList();
        var changes = prices.SkipLast(1).Zip(prices.Skip(1)).Select(pair => pair.Second / pair.First);
        return changes.Any(change => change > 1.5m);
    }

    private sealed record TradingSymbolData(TradingSymbol Symbol, IReadOnlyList<DailyTradingData> TradingData);
}
