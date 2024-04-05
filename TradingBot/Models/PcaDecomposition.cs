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
    public required Vector<double> L1Norms { get; init; }
    public required Vector<double> L2Norms { get; init; }
    public required Matrix<double> PrincipalVectors { get; init; }

    public IReadOnlyList<SymbolWithNormalizedDifference> CalculatePriceDifferences(
        IReadOnlyDictionary<TradingSymbol, decimal> lastPrices)
    {
        var missingSymbols = Symbols.Where(symbol => !lastPrices.ContainsKey(symbol)).ToList();
        if (missingSymbols.Count > 0)
            throw new ArgumentException(
                $"Last prices dictionary did not contain some symbols: {string.Join(", ", missingSymbols.Select(s => s.Value))}",
                nameof(lastPrices));

        var newData = DenseVector.OfEnumerable(Symbols.Select(symbol => (double)lastPrices[symbol]));
        var normalizedData = (newData - Means).PointwiseDivide(StandardDeviations);

        var reduced = PrincipalVectors * (PrincipalVectors.Transpose() * normalizedData);

        var predictedPrices = reduced.PointwiseMultiply(StandardDeviations) + Means;
        var normalizedDifferences = (newData - predictedPrices).PointwiseDivide(StandardDeviations);

        return Symbols.Zip(normalizedDifferences)
            .Select(pair => new SymbolWithNormalizedDifference(pair.First, pair.Second)).ToList();
    }

    public IReadOnlyDictionary<TradingSymbol, Norms> GetNorms()
    {
        return Symbols.Zip(L1Norms, L2Norms).ToDictionary(set => set.First, set => new Norms(set.Second, set.Third));
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
            L1Norms = JsonSerializer.Serialize(L1Norms.ToArray()),
            L2Norms = JsonSerializer.Serialize(L2Norms.ToArray()),
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
            L1Norms = DenseVector.OfArray(JsonSerializer.Deserialize<double[]>(entity.L1Norms)),
            L2Norms = DenseVector.OfArray(JsonSerializer.Deserialize<double[]>(entity.L2Norms)),
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

public sealed record Norms(double L1, double L2);
