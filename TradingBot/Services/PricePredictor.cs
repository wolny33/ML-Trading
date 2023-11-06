using TradingBot.Models;

namespace TradingBot.Services;

public interface IPricePredictor
{
    public Task<IDictionary<TradingSymbol, Prediction>> GetPredictionsAsync();
}

public sealed class PricePredictor : IPricePredictor
{
    private readonly IMarketDataSource _marketData;

    public PricePredictor(IMarketDataSource marketData)
    {
        _marketData = marketData;
    }

    public async Task<IDictionary<TradingSymbol, Prediction>> GetPredictionsAsync()
    {
        return await Task.FromException<IDictionary<TradingSymbol, Prediction>>(new NotImplementedException());
    }
}
