using Alpaca.Markets;
using Newtonsoft.Json;
using System.Formats.Asn1;
using System.Globalization;
using System.Net;
using TradingBot.Models;
using ILogger = Serilog.ILogger;
using CsvHelper;
using CsvHelper.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

    Task InitializeBacktestDataAsync(DateOnly start, DateOnly end, BacktestSymbolSlice slice,
        CancellationToken token = default);
}

public sealed class MarketDataSource : IMarketDataSource, IAsyncDisposable
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IMarketDataCache _cache;
    private readonly IAlpacaCallQueue _callQueue;
    private readonly Lazy<Task<IAlpacaDataClient>> _dataClient;
    private readonly ILogger _logger;
    private readonly Lazy<Task<IAlpacaTradingClient>> _tradingClient;
    private readonly ICurrentTradingTask _tradingTask;

    public MarketDataSource(IAlpacaClientFactory clientFactory, IAssetsDataSource assetsDataSource, ILogger logger,
        IMarketDataCache cache, IAlpacaCallQueue callQueue, ICurrentTradingTask tradingTask)
    {
        _assetsDataSource = assetsDataSource;
        _cache = cache;
        _callQueue = callQueue;
        _tradingTask = tradingTask;
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

        var valid = await GetValidSymbolsAsync(_tradingTask.SymbolSlice, token);
        var interestingValidSymbols = (await GetInterestingSymbolsAsync(token)).Where(s => valid.Contains(s));
        var fearGreedIndexes = await GetFearGreedIndexData(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
        return await interestingValidSymbols.Chunk(15)
            .ToAsyncEnumerable()
            .SelectManyAwait(async chunk =>
                (await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, fearGreedIndexes, token)))).ToAsyncEnumerable())
            .Where(pair => IsDataValid(pair.TradingData)).Take(100)
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesForAllSymbolsAsync(
        DateOnly start, DateOnly end, BacktestSymbolSlice? slice = null, CancellationToken token = default)
    {
        _logger.Debug("Getting prices for all symbols from {Start} to {End}", start, end);

        var valid = await GetValidSymbolsAsync(slice ?? _tradingTask.SymbolSlice, token);
        var fearGreedIndexes = await GetFearGreedIndexData(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"), token);
        return await valid.Chunk(50)
            .ToAsyncEnumerable()
            .SelectManyAwait(async chunk =>
            {
                _logger.Verbose("Getting data for chunk: {Symbols}", chunk.Select(t => t.Value).ToList());
                var dataForChunk = await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, fearGreedIndexes, token)));
                return dataForChunk.ToAsyncEnumerable();
            })
            .Where(pair => IsDataValid(pair.TradingData))
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    public async Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol,
        DateOnly start, DateOnly end, CancellationToken token = default)
    {
        var fearGreedIndexes = await GetFearGreedIndexData(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
        var data = await GetSymbolDataAsync(symbol, start, end, fearGreedIndexes, token);
        return IsDataValid(data.TradingData) ? data.TradingData : null;
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
        CancellationToken token = default)
    {
        _logger.Debug("Initializing cache for backtest (from {Start} to {End})", start, end);

        var valid = await GetValidSymbolsAsync(slice, token);
        var fearGreedIndexes = await GetFearGreedIndexData(start.ToString("yyyy-MM-dd"), end.ToString("yyyy-MM-dd"));
        await valid.Chunk(50).ToAsyncEnumerable().SelectManyAwait(async chunk =>
            {
                _logger.Verbose("Getting data for chunk: {Symbols}", chunk.Select(t => t.Value).ToList());
                var dataForChunk = await Task.WhenAll(chunk.Select(s => GetSymbolDataAsync(s, start, end, fearGreedIndexes, token)));
                return dataForChunk.ToAsyncEnumerable();
            })
            .ToListAsync(token);

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
            ? Task.FromResult(_cache.GetMostActiveCachedSymbolsForLastValidDay(_tradingTask.GetTaskDay()))
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
        IReadOnlyDictionary<DateTime, double> fearGreedIndexes, CancellationToken token = default)
    {
        if (_cache.TryGetCachedData(symbol, start, end) is { } cached)
        {
            _logger.Verbose("Retrieved {Token} data between {Start} and {End} from cache", symbol.Value, start, end);
            return new TradingSymbolData(symbol, cached);
        }

        var bars = await SendBarsRequestAsync(symbol, start, end, fearGreedIndexes, token);
        _cache.CacheDailySymbolData(symbol, bars, start, end);
        _logger.Verbose("Retrieved {Token} data between {Start} and {End} from Alpaca", symbol.Value, start, end);
        return new TradingSymbolData(symbol, bars);
    }

    private async Task<IReadOnlyList<DailyTradingData>> SendBarsRequestAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, IReadOnlyDictionary<DateTime, double> fearGreedIndexes, CancellationToken token = default)
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
                    Volume = b.Volume,
                    FearGreedIndex = (decimal)fearGreedIndexes[b.TimeUtc.Date]
                }).ToList();
            } while (nextPageToken is not null);
        }
    }

    private async Task<Dictionary<DateTime, double>> GetFearGreedIndexData(string startDate, string endDate, CancellationToken token = default)
    {
        _logger.Debug("Getting Fear Greed Index for all symbols from {Start} to {End}", startDate, endDate);
        var startDateTime = DateTime.Parse(startDate);
        var endDateTime = DateTime.Parse(endDate);
        var splitDateTime = new DateTime(2020, 7, 15);
        var firstDateTime = new DateTime(2011, 1, 3);
        var fearGreedIndexes = new Dictionary<DateTime, double>();

        if(startDateTime < firstDateTime)
        {
            for (var date = startDateTime; date < firstDateTime; date = date.AddDays(1))
            {
                fearGreedIndexes.Add(date, 50.0);
            }
        }
        if (startDateTime < splitDateTime)
        {
            var csvData = new Dictionary<DateTime, double>();
            if (endDateTime < splitDateTime)
                csvData = await GetFearGreedIndexDataFromCsv(startDate, endDate, token);
            else
                csvData = await GetFearGreedIndexDataFromCsv(startDate, "2020-07-14", token);
            foreach (var fearGreedIndexForDate in csvData)
            {
                fearGreedIndexes.Add(fearGreedIndexForDate.Key, fearGreedIndexForDate.Value);
            }
        }

        if (endDateTime >= splitDateTime)
        {
            var apiData = new Dictionary<DateTime, double>();
            if (startDateTime >= splitDateTime)
                apiData = await GetFearGreedIndexDataFromApi(startDate, endDate, token);
            else
                apiData = await GetFearGreedIndexDataFromApi("2020-07-15", endDate, token);
            foreach (var fearGreedIndexForDate in apiData)
            {
                fearGreedIndexes.Add(fearGreedIndexForDate.Key, fearGreedIndexForDate.Value);
            }
        }
        return fearGreedIndexes;
    }

    private async Task<Dictionary<DateTime, double>> GetFearGreedIndexDataFromApi(string startDate, string endDate, CancellationToken token = default)
    {
        var url = $"https://production.dataviz.cnn.io/index/fearandgreed/graphdata/{startDate}";
        var fearGreedIndexes = new Dictionary<DateTime, double>();
        try
        {
            using (var handler = new HttpClientHandler())
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0");

                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        if (responseJson != null)
                        {
                            var fearGreedIndexData = responseJson["fear_and_greed_historical"]["data"];
                            var end = DateTime.Parse(endDate);

                            foreach (var data in fearGreedIndexData)
                            {
                                var unixTimeStampInMilliseconds = double.Parse((string)data.x);
                                var date = DateTimeOffset.FromUnixTimeMilliseconds((long)unixTimeStampInMilliseconds).DateTime;

                                if (date <= end)
                                {
                                    fearGreedIndexes.Add(date, (double)data.y);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "An exception occured when obtaining FearGreedIndex from api");

            var startDateTime = DateTime.Parse(startDate);
            var endDateTime = DateTime.Parse(endDate);
            for (var date = startDateTime; date <= endDateTime; date = date.AddDays(1))
            {
                fearGreedIndexes.Add(date, 50.0);
            }
        }
        return fearGreedIndexes;
    }

    private async Task<Dictionary<DateTime, double>> GetFearGreedIndexDataFromCsv(string startDate, string endDate, CancellationToken token = default)
    {
        var fearGreedIndexes = new Dictionary<DateTime, double>();

        try
        {
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync("https://raw.githubusercontent.com/hackingthemarkets/sentiment-fear-and-greed/master/datasets/fear-greed.csv"))
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                            {
                                HasHeaderRecord = true,
                                MissingFieldFound = null,
                                Delimiter = ","
                            };

                            using (var csv = new CsvReader(reader, csvConfig))
                            {
                                csv.Read();
                                csv.ReadHeader();

                                while (csv.Read())
                                {
                                    var date = DateTime.Parse(csv.GetField("Date"));
                                    if (date >= DateTime.Parse(startDate) && date <= DateTime.Parse(endDate))
                                    {
                                        fearGreedIndexes.Add(date, double.Parse(csv.GetField("Fear Greed")));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "An exception occured when obtaining FearGreedIndex from csv");

            var startDateTime = DateTime.Parse(startDate);
            var endDateTime = DateTime.Parse(endDate);
            for (var date = startDateTime; date <= endDateTime; date = date.AddDays(1))
            {
                fearGreedIndexes.Add(date, 50.0);
            }
        }
        return fearGreedIndexes;
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

        bool HasInvalidFearGreedIndexValue(DailyTradingData data)
        {
            return data.FearGreedIndex < 0 || data.FearGreedIndex > 100;
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

            if (HasInvalidFearGreedIndexValue(d))
            {
                _logger.Warning("Market data entry is invalid - fear greed index is not correct: {Entry}", d);
                return false;
            }
        }

        return true;
    }

    private sealed record TradingSymbolData(TradingSymbol Symbol, IReadOnlyList<DailyTradingData> TradingData);
}
