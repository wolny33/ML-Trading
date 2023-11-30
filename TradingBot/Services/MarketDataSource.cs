using System.Net.Sockets;
using Alpaca.Markets;
using Flurl.Http;
using Newtonsoft.Json;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IMarketDataSource
{
    public Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default);

    public Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, CancellationToken token = default);
}

public sealed class MarketDataSource : IMarketDataSource
{
    private readonly IAlpacaClientFactory _clientFactory;
    private readonly IAssetsDataSource _assetsDataSource;

    public MarketDataSource(IAlpacaClientFactory clientFactory, IAssetsDataSource assetsDataSource)
    {
        _clientFactory = clientFactory;
        _assetsDataSource = assetsDataSource;
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        var valid = await GetValidSymbolsAsync(token);
        
        using var dataClient = await _clientFactory.CreateMarketDataClientAsync(token);
        
        // ReSharper disable once AccessToDisposedClosure
        return await (await GetInterestingSymbolsAsync(60, token)).Where(s => valid.Contains(s)).ToAsyncEnumerable()
            .SelectAwait(async s =>
                new TradingSymbolData(s, await GetDataForSymbolAsync(s, start, end, dataClient, token)))
            .Where(pair => IsDataValid(pair.TradingData, 11)) // TODO: Improve length validation
            .Take(15)
            .ToDictionaryAsync(pair => pair.Symbol, pair => pair.TradingData, token);
    }

    private async Task<ISet<TradingSymbol>> GetValidSymbolsAsync(CancellationToken token = default)
    {
        using var assetsClient = _clientFactory.CreateAvailableAssetsClient();

        try
        {
            var response = await assetsClient.Request().AllowAnyHttpStatus().GetAsync(token);
            var availableAssets = response.StatusCode switch
            {
                StatusCodes.Status200OK => await response.GetJsonAsync<IReadOnlyList<AssetResponse>>(),
                var status => throw new UnsuccessfulAlpacaResponseException(status, await response.GetStringAsync())
            };
            return availableAssets.Where(a => a is { Fractionable: true, Tradable: true })
                .Select(a => new TradingSymbol(a.Symbol)).ToHashSet();
        }
        catch (Exception e) when (e is HttpRequestException or SocketException or TaskCanceledException)
        {
            throw new AlpacaCallFailedException(e);
        }
    }

    private async Task<IEnumerable<TradingSymbol>> GetInterestingSymbolsAsync(int count, CancellationToken token = default)
    {
        using var dataClient = await _clientFactory.CreateMarketDataClientAsync(token);

        var held = (await _assetsDataSource.GetAssetsAsync(token)).Positions.Keys.ToList();
        var active =
            (await dataClient.ListMostActiveStocksByVolumeAsync(count - held.Count, token)).Select(a =>
                new TradingSymbol(a.Symbol));

        return held.Concat(active);
    }

    private static async Task<IReadOnlyList<DailyTradingData>> GetDataForSymbolAsync(TradingSymbol symbol,
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

    private static bool IsDataValid(IReadOnlyList<DailyTradingData> dailyData, int? minLength = null)
    {
        bool HasNonPositiveValues(DailyTradingData data)
        {
            return data.Open <= 0 || data.Close <= 0 || data.High <= 0 || data.Low <= 0 || data.Volume <= 0;
        }

        bool HasInvalidHighAndLowPrices(DailyTradingData data)
        {
            return data.Low > data.Open || data.Low > data.Close || data.High < data.Open || data.High < data.Close;
        }

        return (minLength is null || dailyData.Count >= minLength.Value) &&
               !dailyData.Any(d => HasNonPositiveValues(d) || HasInvalidHighAndLowPrices(d));
    }

    public async Task<IReadOnlyList<DailyTradingData>?> GetDataForSingleSymbolAsync(TradingSymbol symbol, DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        using var client = await _clientFactory.CreateMarketDataClientAsync(token);
        var data = await GetDataForSymbolAsync(symbol, start, end, client, token);

        return IsDataValid(data) ? data : null;
    }

    private sealed record TradingSymbolData(TradingSymbol Symbol, IReadOnlyList<DailyTradingData> TradingData);
    
    private sealed class AssetResponse
    {
        public required Guid Id { get; init; }
        public required string Exchange { get; init; }
        public required string Class { get; init; }
        public required string Symbol { get; init; }
        public required string Name { get; init; }
        public required string Status { get; init; }
        public required bool Tradable { get; init; }
        public required bool Marginable { get; init; }
        
        [JsonProperty("maintenance_margin_requirement")]
        public required int MaintenanceMarginRequirement { get; set; }
        
        public required bool Shortable { get; init; }
        
        [JsonProperty("easy_to_borrow")]
        public required bool EasyToBorrow { get; init; }
        public required bool Fractionable { get; init; }
    }
}
