using TradingBot.Models;

namespace TradingBot.Services;

public interface IMarketDataSource
{
    public Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetAllPricesAsync(DateOnly start,
        DateOnly end);
}

public sealed class MarketDataSource : IMarketDataSource
{
    private readonly IAlpacaClientFactory _clientFactory;

    public MarketDataSource(IAlpacaClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>> GetAllPricesAsync(DateOnly start,
        DateOnly end)
    {
        return await Task.FromException<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>>(
            new NotImplementedException());
    }
}
