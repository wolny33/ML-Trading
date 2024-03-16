using System.Text.Json;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class PcaDecomposition
{
    public static Guid NormalExecutionId => Guid.NewGuid();

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

    public PcaDecompositionEntity ToEntity(Guid? backtestId)
    {
        return new PcaDecompositionEntity
        {
            Id = Guid.NewGuid(),
            BacktestId = backtestId ?? NormalExecutionId,
            CreationTimestamp =
                new DateTimeOffset(CreatedAt.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), TimeSpan.Zero)
                    .ToUnixTimeMilliseconds(),
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
            Symbols = string.Join(';', Symbols.Select(s => s.Value)),
            Means = JsonSerializer.Serialize(Means.ToArray()),
            StandardDeviations = JsonSerializer.Serialize(StandardDeviations.ToArray()),
            PrincipalVectors = JsonSerializer.Serialize(PrincipalVectors.ToArray())
        };
    }

    public static PcaDecomposition FromEntity(PcaDecompositionEntity entity)
    {
        return new PcaDecomposition
        {
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt,
            Symbols = entity.Symbols.Split(';').Select(v => new TradingSymbol(v)).ToList(),
            Means = DenseVector.OfArray(JsonSerializer.Deserialize<double[]>(entity.Means)),
            StandardDeviations = DenseVector.OfArray(JsonSerializer.Deserialize<double[]>(entity.StandardDeviations)),
            PrincipalVectors = DenseMatrix.OfArray(JsonSerializer.Deserialize<double[,]>(entity.PrincipalVectors))
        };
    }
}

public sealed record SymbolWithNormalizedDifference(TradingSymbol Symbol, double NormalizedDifference);
