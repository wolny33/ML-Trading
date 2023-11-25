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
        // This method should ensure that data is valid (i.e. no <=0 values)
        return await Task.FromException<IDictionary<TradingSymbol, IReadOnlyList<DailyTradingData>>>(
            new NotImplementedException());
    }
}
