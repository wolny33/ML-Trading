using System.Collections.Concurrent;
using TradingBot.Models;

namespace TradingBot.Services;

public interface IExcludedBacktestSymbols
{
    void Set(Guid backtestId, IReadOnlyList<TradingSymbol> symbols);
    IReadOnlySet<TradingSymbol> Get(Guid backtestId);
}

public sealed class ExcludedBacktestSymbols : IExcludedBacktestSymbols
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<TradingSymbol>> _excludedSymbols = new();

    public void Set(Guid backtestId, IReadOnlyList<TradingSymbol> symbols)
    {
        _excludedSymbols[backtestId] = symbols;
    }

    public IReadOnlySet<TradingSymbol> Get(Guid backtestId)
    {
        return _excludedSymbols.TryGetValue(backtestId, out var symbols)
            ? symbols.ToHashSet()
            : new HashSet<TradingSymbol>();
    }
}
