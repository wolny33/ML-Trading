using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class PairGroupEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required long CreationTimestamp { get; init; }
    public List<PairEntity> Pairs { get; init; } = new();
}
