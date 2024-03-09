using TradingBot.Models;

namespace TradingBot.Services;

public sealed class BuyLosersStrategy : IStrategy
{
    private readonly IBuyLosersStrategyStateService _stateService;
    private readonly ICurrentTradingTask _tradingTask;

    public BuyLosersStrategy(ICurrentTradingTask tradingTask, IBuyLosersStrategyStateService stateService)
    {
        _tradingTask = tradingTask;
        _stateService = stateService;
    }

    public static string StrategyName => "Overreaction strategy";
    public string Name => StrategyName;

    public Task<IReadOnlyList<TradingAction>> GetTradingActionsAsync(CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
