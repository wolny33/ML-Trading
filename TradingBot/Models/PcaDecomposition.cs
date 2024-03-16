using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace TradingBot.Models;

public sealed class PcaDecomposition
{
    public required DateOnly CreatedAt { get; init; }
    public required DateOnly ExpiresAt { get; init; }
    public required IReadOnlyList<TradingSymbol> Symbols { get; init; }
    public required Vector<double> Means { get; init; }
    public required Vector<double> StandardDeviations { get; init; }
    public required Matrix<double> PrincipalVectors { get; init; }

    public IReadOnlyList<SymbolWithNormalizedDifference> CalculatePriceDifferences(
        IReadOnlyDictionary<TradingSymbol, decimal> lastPrices)
    {
        var filteredIndices = Symbols
            .Select((symbol, index) => new { Symbol = symbol, Index = index })
            .Where(pair => lastPrices.ContainsKey(pair.Symbol))
            .ToList();

        var newData = DenseVector.OfEnumerable(filteredIndices.Select(pair => (double)lastPrices[pair.Symbol]));
        var means = DenseVector.OfEnumerable(filteredIndices.Select(pair => Means[pair.Index]));
        var stdDeviations =
            DenseVector.OfEnumerable(filteredIndices.Select(pair => StandardDeviations[pair.Index]));

        var filteredVectors =
            DenseMatrix.OfRowVectors(filteredIndices.Select(pair => PrincipalVectors.Row(pair.Index)));

        var normalizedData = (newData - means).PointwiseDivide(stdDeviations);
        var reduced = filteredVectors * (filteredVectors.Transpose() * normalizedData);

        var predictedPrices = reduced.PointwiseMultiply(stdDeviations) + means;
        var normalizedDifferences = (newData - predictedPrices).PointwiseDivide(stdDeviations);

        return filteredIndices.Select((pair, index) =>
            new SymbolWithNormalizedDifference(pair.Symbol, normalizedDifferences[index])).ToList();
    }
}

public sealed record SymbolWithNormalizedDifference(TradingSymbol Symbol, double NormalizedDifference);
