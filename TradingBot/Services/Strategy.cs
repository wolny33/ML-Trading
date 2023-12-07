using TradingBot.Models;

namespace TradingBot.Services;

public interface IStrategy
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync();
}

public sealed class Strategy : IStrategy
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IPricePredictor _predictor;

    public Strategy(IPricePredictor predictor, IAssetsDataSource assetsDataSource)
    {
        _predictor = predictor;
        _assetsDataSource = assetsDataSource;
    }

    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync()
    {
        return Task.FromResult<IReadOnlyList<TradingAction>>(Array.Empty<TradingAction>());
    }
}
