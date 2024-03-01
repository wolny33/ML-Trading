using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class PairGroup
{
    public required Guid Id { get; init; }
    public required DateTimeOffset DeterminedAt { get; init; }
    public required IReadOnlyList<Pair> Pairs { get; init; }

    public static PairGroup FromEntity(PairGroupEntity entity)
    {
        return new PairGroup
        {
            Id = entity.Id,
            DeterminedAt = DateTimeOffset.FromUnixTimeMilliseconds(entity.CreationTimestamp),
            Pairs = entity.Pairs.Select(Pair.FromEntity).ToList()
        };
    }

    public PairGroupEntity ToEntity()
    {
        return new PairGroupEntity
        {
            Id = Id,
            CreationTimestamp = DeterminedAt.ToUnixTimeMilliseconds(),
            Pairs = Pairs.Select(p => p.ToEntity(Id)).ToList()
        };
    }
}

public sealed record Pair(TradingSymbol First, TradingSymbol Second)
{
    public static Pair CreateOrdered(TradingSymbol first, TradingSymbol second)
    {
        if (string.Compare(first.Value, second.Value, StringComparison.Ordinal) < 0)
        {
            (first, second) = (second, first);
        }

        return new Pair(first, second);
    }

    public static Pair FromEntity(PairEntity entity)
    {
        return new Pair(new(entity.FirstToken), new(entity.SecondToken));
    }

    public PairEntity ToEntity(Guid groupId)
    {
        return new PairEntity
        {
            Id = Guid.NewGuid(),
            PairGroupId = groupId,
            FirstToken = First.Value,
            SecondToken = Second.Value
        };
    }
}
