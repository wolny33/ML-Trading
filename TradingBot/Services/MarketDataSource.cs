namespace TradingBot.Services;

public interface IMarketDataSource
{
    public Task GetAllPricesAsync(DateTimeOffset start, DateTimeOffset end);
}

public sealed class MarketDataSource : IMarketDataSource
{
    private readonly IAlpacaClientFactory _clientFactory;

    public MarketDataSource(IAlpacaClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task GetAllPricesAsync(DateTimeOffset start, DateTimeOffset end)
    {
        await Task.FromException(new NotImplementedException());
    }
}
