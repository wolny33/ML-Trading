using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class PositionEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required Guid SymbolId { get; init; }
    public required string Symbol { get; init; }
    public required double Quantity { get; init; }
    public required double AvailableQuantity { get; init; }
    public required double MarketValue { get; init; }
    public required double AverageEntryPrice { get; init; }

    [Required]
    public AssetsStateEntity AssetsState { get; init; } = null!;

    public required Guid AssetsStateId { get; init; }
}
