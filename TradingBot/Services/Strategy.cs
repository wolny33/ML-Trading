using TradingBot.Models;

namespace TradingBot.Services;

public interface IStrategy
{
    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync();
}

public sealed class Strategy : IStrategy
{
    private readonly IPricePredictor _predictor;

    public Strategy(IPricePredictor predictor)
    {
        _predictor = predictor;
    }

    public async Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync()
    {
        return await Task.FromException<IReadOnlyList<TradingAction>>(new NotImplementedException());
    }
}
