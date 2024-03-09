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
    private string? _selectedName;

    public StrategyFactory(IEnumerable<IStrategy> strategies, IStrategySelectionService strategySelection)
    {
        _strategySelection = strategySelection;
        _strategies = strategies.ToDictionary(strategy => strategy.Name);
    }

    public async Task<IStrategy> CreateAsync(CancellationToken token = default)
    {
        _selectedName ??= await _strategySelection.GetSelectedNameAsync(token);

        if (!_strategies.TryGetValue(_selectedName, out var strategy))
        {
            throw new UnreachableException($"Selected name doe not correspond to a strategy: '{_selectedName}'");
        }

        return strategy;
    }
}
