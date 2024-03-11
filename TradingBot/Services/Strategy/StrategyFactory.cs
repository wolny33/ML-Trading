using System.Diagnostics;

namespace TradingBot.Services.Strategy;

public interface IStrategyFactory
{
    Task<IStrategy> CreateAsync(CancellationToken token = default);
}

public sealed class StrategyFactory : IStrategyFactory
{
    private readonly IReadOnlyDictionary<string, IStrategy> _strategies;
    private readonly IStrategySelectionService _strategySelection;
    private readonly ICurrentTradingTask _tradingTask;

    public StrategyFactory(IEnumerable<IStrategy> strategies, IStrategySelectionService strategySelection,
        ICurrentTradingTask tradingTask)
    {
        _strategySelection = strategySelection;
        _tradingTask = tradingTask;
        _strategies = strategies.ToDictionary(strategy => strategy.Name);
    }

    public async Task<IStrategy> CreateAsync(CancellationToken token = default)
    {
        var selectedName = await _strategySelection.GetSelectedNameAsync(_tradingTask.CurrentBacktestId, token);

        if (!_strategies.TryGetValue(selectedName, out var strategy))
        {
            throw new UnreachableException($"Selected name does not correspond to a strategy: '{selectedName}'");
        }

        return strategy;
    }
}
