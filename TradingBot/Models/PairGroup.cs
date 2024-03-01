using TradingBot.Database.Entities;

namespace TradingBot.Models;

public sealed class PairGroup
{
    public required DateTimeOffset DeterminedAt { get; init; }
    public required IReadOnlyList<Pair> Pairs { get; init; }

    public static PairGroup FromEntity(PairGroupEntity entity)
    {
        return new PairGroup
        {
            DeterminedAt = DateTimeOffset.FromUnixTimeMilliseconds(entity.CreationTimestamp),
            Pairs = entity.Pairs.Select(Pair.FromEntity).ToList()
        };
    }

    public PairGroupEntity ToEntity()
    {
        var id = Guid.NewGuid();
        return new PairGroupEntity
        {
            Id = id,
            CreationTimestamp = DeterminedAt.ToUnixTimeMilliseconds(),
            Pairs = Pairs.Select(p => p.ToEntity(id)).ToList()
        };
    }
}

public sealed record Pair(TradingSymbol First, TradingSymbol Second)
{
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
