using Alpaca.Markets;
using Flurl.Http;
using Newtonsoft.Json;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IMarketDataSource
{
    public Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetAllPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default);
}

public sealed class MarketDataSource : IMarketDataSource
{
    private readonly IAlpacaClientFactory _clientFactory;

    public MarketDataSource(IAlpacaClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetAllPricesAsync(DateOnly start,
        DateOnly end, CancellationToken token = default)
    {
        // This method should ensure that data is valid (i.e. no <=0 values)
        
        using var assetsClient = _clientFactory.CreateAvailableAssetsClient();

        var response = await assetsClient.Request().SetQueryParams(new
        {
            status = "active",
            asset_class = "us_equity"
        }).GetAsync(token);
        var availableAssets = response.StatusCode switch
        {
            StatusCodes.Status200OK => await response.GetJsonAsync<IReadOnlyList<AssetResponse>>(),
            var status => throw new UnsuccessfulAlpacaResponseException(status, await response.GetStringAsync())
        };

        var validAssets = availableAssets.Where(a => a is { Fractionable: true, Tradable: true }).ToList();
        var symbols = validAssets.Select(a => new SymbolWithId(a.Symbol, a.Id)).ToList();
        
        using var client = await _clientFactory.CreateMarketDataClientAsync(token);
        var bars = await client.ListHistoricalBarsAsync(
            new HistoricalBarsRequest(symbols[0].Symbol, new BarTimeFrame(1, BarTimeFrameUnit.Day),
                new Interval<DateTime>(DateTime.Now - TimeSpan.FromDays(5), DateTime.Now - TimeSpan.FromHours(1))), token);

        return await Task.FromException<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>>(
            new NotImplementedException());
    }

    private sealed record SymbolWithId(string Symbol, Guid Id);
    
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
