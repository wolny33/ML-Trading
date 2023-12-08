using Microsoft.AspNetCore.Authentication;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IStrategy
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync();
}

public sealed class Strategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly ISystemClock _clock;
    private readonly IPricePredictor _predictor;

    public Strategy(IPricePredictor predictor, IAssetsDataSource assetsDataSource, ISystemClock clock)
    {
        _predictor = predictor;
        _assetsDataSource = assetsDataSource;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync()
    {
        var predictions = await _predictor.GetPredictionsAsync();
        var assets = await _assetsDataSource.GetAssetsAsync();
        return new[]
        {
            TradingAction.MarketBuy(new TradingSymbol("TSLA"), 0.5m, _clock.UtcNow)
        };
    }
}
