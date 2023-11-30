using System.Net.Sockets;
using Alpaca.Markets;
using TradingBot.Models;
using TradingBot.Services.Alpaca;

namespace TradingBot.Services;

public interface IMarketDataSource
{
    Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default);

    Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, CancellationToken token = default);
}

public sealed class MarketDataSource : IMarketDataSource
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IAlpacaClientFactory _clientFactory;

    public MarketDataSource(IAlpacaClientFactory clientFactory, IAssetsDataSource assetsDataSource)
    {
        _clientFactory = clientFactory;
        _assetsDataSource = assetsDataSource;
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        var valid = await SendValidSymbolsRequestAsync(token);

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

    private async Task<ISet<TradingSymbol>> SendValidSymbolsRequestAsync(CancellationToken token = default)
    {
        using var assetsClient = _clientFactory.CreateAvailableAssetsClient();

        try
        {
            var availableAssets = await assetsClient.GetAvailableAssetsAsync(token);
            return availableAssets.Where(a => a is { Fractionable: true, Tradable: true })
                .Select(a => new TradingSymbol(a.Symbol)).ToHashSet();
        }
        catch (Exception e) when (e is HttpRequestException or SocketException or TaskCanceledException)
        {
            throw new AlpacaCallFailedException(e);
        }
    }

    private async Task<IEnumerable<TradingSymbol>> SendInterestingSymbolsRequestsAsync(
        CancellationToken token = default)
    {
        const int maxRequestSize = 100;
        using var dataClient = await _clientFactory.CreateMarketDataClientAsync(token);

        var held = (await _assetsDataSource.GetAssetsAsync(token)).Positions.Keys.ToList();
        var active =
            (await dataClient.ListMostActiveStocksByVolumeAsync(maxRequestSize, token)).Select(a =>
                new TradingSymbol(a.Symbol));

        return held.Concat(active);
    }

    private static async Task<IReadOnlyList<DailyTradingData>> SendBarsRequestAsync(TradingSymbol symbol,
        DateOnly start, DateOnly end, IAlpacaDataClient client, CancellationToken token = default)
    {
        var startTime = start.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var endTime = end.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero));
        var interval = new Interval<DateTime>(startTime, endTime);
        var barTimeFrame = new BarTimeFrame(1, BarTimeFrameUnit.Day);

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
        catch (RestClientErrorException e)
        {
            throw new UnsuccessfulAlpacaResponseException(e.HttpStatusCode is not null ? (int)e.HttpStatusCode : 0,
                e.Message);
        }
        catch (Exception e) when (e is HttpRequestException or SocketException or TaskCanceledException)
        {
            throw new AlpacaCallFailedException(e);
        }
    }

    private static bool IsDataValid(IReadOnlyList<DailyTradingData> dailyData)
    {
        bool HasNonPositiveValues(DailyTradingData data)
        {
            return data.Open <= 0 || data.Close <= 0 || data.High <= 0 || data.Low <= 0 || data.Volume <= 0;
        }

        bool HasInvalidHighAndLowPrices(DailyTradingData data)
        {
            return data.Low > data.Open || data.Low > data.Close || data.High < data.Open || data.High < data.Close;
        }

        return dailyData.Any() && !dailyData.Any(d => HasNonPositiveValues(d) || HasInvalidHighAndLowPrices(d));
    }

    private sealed record TradingSymbolData(TradingSymbol Symbol, IReadOnlyList<DailyTradingData> TradingData);
}
