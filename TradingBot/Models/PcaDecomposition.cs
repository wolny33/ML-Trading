using System.Diagnostics;
using System.Text.Json;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using TradingBot.Database.Entities;

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

        if (filteredIndices.Count == 0) return Array.Empty<SymbolWithNormalizedDifference>();

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
            BacktestId = backtestId,
            CreationTimestamp =
                new DateTimeOffset(CreatedAt.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.Zero)), TimeSpan.Zero)
                    .ToUnixTimeMilliseconds(),
            CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt,
            Symbols = string.Join(';', Symbols.Select(s => s.Value)),
            Means = JsonSerializer.Serialize(Means.ToArray()),
            StandardDeviations = JsonSerializer.Serialize(StandardDeviations.ToArray()),
            PrincipalVectors = JsonSerializer.Serialize(ConvertToJaggedArray(PrincipalVectors.ToArray()))
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
            PrincipalVectors = DenseMatrix.OfArray(ConvertTo2DArray(
                JsonSerializer.Deserialize<double[][]>(entity.PrincipalVectors) ??
                throw new UnreachableException(
                    $"{nameof(entity.PrincipalVectors)} saved in database have invalid format")))
        };
    }

    private static double[][] ConvertToJaggedArray(double[,] multiArray)
    {
        var rows = multiArray.GetLength(0);
        var cols = multiArray.GetLength(1);
        var jaggedArray = new double[rows][];

        for (var i = 0; i < rows; i++)
        {
            jaggedArray[i] = new double[cols];
            for (var j = 0; j < cols; j++) jaggedArray[i][j] = multiArray[i, j];
        }

        return jaggedArray;
    }

    private static double[,] ConvertTo2DArray(double[][] jaggedArray)
    {
        var rows = jaggedArray.Length;
        if (rows <= 0) return new double[0, 0];

        var cols = jaggedArray[0].Length;
        var multiArray = new double[rows, cols];

        for (var i = 0; i < rows; i++)
            for (var j = 0; j < cols; j++)
                multiArray[i, j] = jaggedArray[i][j];

        return multiArray;
    }
}

public sealed record SymbolWithNormalizedDifference(TradingSymbol Symbol, double NormalizedDifference);
