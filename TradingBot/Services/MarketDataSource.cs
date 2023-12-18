using System.Net.Sockets;
using Alpaca.Markets;
using TradingBot.Exceptions;
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
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly ILogger _logger;

    public MarketDataSource(IAlpacaClientFactory clientFactory, IAssetsDataSource assetsDataSource, ILogger logger)
    {
        _clientFactory = clientFactory;
        _assetsDataSource = assetsDataSource;
        _logger = logger.ForContext<MarketDataSource>();
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        _logger.Debug("Getting prices from {Start} to {End}", start, end);

        var valid = await SendValidSymbolsRequestAsync(token);
        _logger.Debug("Retrieved {Count} valid trading symbols", valid.Count);

        using var dataClient = await _clientFactory.CreateMarketDataClientAsync(token);

        // ReSharper disable once AccessToDisposedClosure
        return await (await SendInterestingSymbolsRequestsAsync(token)).Where(s => valid.Contains(s))
            .ToAsyncEnumerable()
            .SelectAwait(async s =>
                new TradingSymbolData(s, await SendBarsRequestAsync(s, start, end, dataClient, token)))
            .Where(pair => IsDataValid(pair.TradingData))
            .Take(15)
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    public async Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol,
        DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        using var client = await _clientFactory.CreateMarketDataClientAsync(token);
        var data = await SendBarsRequestAsync(symbol, start, end, client, token);

        return IsDataValid(data) ? data : null;
    }

    public async Task<decimal> GetLastAvailablePriceForSymbolAsync(TradingSymbol symbol,
        CancellationToken token = default)
    {
        using var client = await _clientFactory.CreateMarketDataClientAsync(token);
        try
        {
            var latestTradeData = await client.GetLatestTradeAsync(new LatestMarketDataRequest(symbol.Value), token);
            return latestTradeData.Price;
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            _logger.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            _logger.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
    }

    private async Task<ISet<TradingSymbol>> SendValidSymbolsRequestAsync(CancellationToken token = default)
    {
        using var tradingClient = await _clientFactory.CreateTradingClientAsync(token);

        try
        {
            var availableAssets = await tradingClient.ListAssetsAsync(
                new AssetsRequest
                {
                    AssetClass = AssetClass.UsEquity,
                    AssetStatus = AssetStatus.Active
                }, token);
            return availableAssets.Where(a => a is { Fractionable: true, IsTradable: true })
                .Select(a => new TradingSymbol(a.Symbol)).ToHashSet();
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            _logger.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            _logger.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
    }

    private async Task<IEnumerable<TradingSymbol>> SendInterestingSymbolsRequestsAsync(
        CancellationToken token = default)
    {
        const int maxRequestSize = 100;
        using var dataClient = await _clientFactory.CreateMarketDataClientAsync(token);

        var held = (await _assetsDataSource.GetAssetsAsync(token)).Positions.Keys.ToList();
        _logger.Debug("Retrieved held tokens: {Tokens}", held.Select(t => t.Value).ToList());

        try
        {
            var active =
                (await dataClient.ListMostActiveStocksByVolumeAsync(maxRequestSize, token)).Select(a =>
                    new TradingSymbol(a.Symbol));
            _logger.Debug("Retrieved most active tokens");

            return held.Concat(active).Distinct();
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            _logger.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            _logger.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
    }

    private async Task<IReadOnlyList<DailyTradingData>> SendBarsRequestAsync(TradingSymbol symbol,
        DateOnly start, DateOnly end, IAlpacaDataClient client, CancellationToken token = default)
    {
        var startTime = start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var endTime = end.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var interval = new Interval<DateTime>(startTime, endTime);
        var barTimeFrame = new BarTimeFrame(1, BarTimeFrameUnit.Day);

        _logger.Verbose("Sending bars request for token {Token} in interval {Start} to {End}", symbol.Value, start,
            end);

        try
        {
            var bars = await client.ListHistoricalBarsAsync(
                new HistoricalBarsRequest(symbol.Value, barTimeFrame, interval), token);

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
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            _logger.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException
                                      or TaskCanceledException)
        {
            _logger.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
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
