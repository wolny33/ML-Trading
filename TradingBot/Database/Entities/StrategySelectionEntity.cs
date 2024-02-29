using System.ComponentModel.DataAnnotations;
using TradingBot.Services;

namespace TradingBot.Database.Entities;

public sealed class StrategySelectionEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public static StrategySelectionEntity MakeDefault()
    {
        return new() { Name = Strategy.StrategyName };
    }
}
