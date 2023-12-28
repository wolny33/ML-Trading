using TradingBot.Models;

namespace TradingBot.Services;

public interface IBacktestActionExecutor
{
    void PostActionForBacktest(TradingAction action, Guid backtestId);
    void ExecuteQueuedActionsForBacktest(Guid backtestId);
}

public sealed class BacktestActionExecutor : IBacktestActionExecutor
{
    private readonly IBacktestAssets _backtestAssets;

    public BacktestActionExecutor(IBacktestAssets backtestAssets)
    {
        _backtestAssets = backtestAssets;
    }

    public void PostActionForBacktest(TradingAction action, Guid backtestId)
    {
        throw new NotImplementedException();
    }

    public void ExecuteQueuedActionsForBacktest(Guid backtestId)
    {
        // TODO: Update action's state in db
        throw new NotImplementedException();
    }
}
