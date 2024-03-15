namespace TradingBot.Models;

public sealed class PcaDecomposition
{
    public required DateOnly CreatedAt { get; init; }
    public required IReadOnlyList<TradingSymbol> Symbols { get; init; }
    public required IReadOnlyList<double> Means { get; init; }
    public required IReadOnlyList<double> Variations { get; init; }
    public required IReadOnlyList<IReadOnlyList<double>> PrincipalVectors { get; init; }
}
