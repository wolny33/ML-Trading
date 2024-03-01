using System.ComponentModel.DataAnnotations;
using TradingBot.Models;

namespace TradingBot.Database.Entities;

public sealed class PairEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required string FirstToken { get; init; }
    public required string SecondToken { get; init; }

    public required Guid PairGroupId { get; init; }

    [Required]
    public PairGroupEntity PairGroup { get; init; } = null!;
}
