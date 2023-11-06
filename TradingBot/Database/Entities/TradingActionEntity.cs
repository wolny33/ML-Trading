using System.ComponentModel.DataAnnotations;
using Alpaca.Markets;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Database.Entities;

public sealed class TradingActionEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required double? Price { get; init; }
    public required double Quantity { get; init; }
    public required string Symbol { get; init; }
    public required TimeInForce InForce { get; init; }
    public required OrderType OrderType { get; init; }

    [Required]
    public TradingActionDetailsEntity Details { get; init; } = null!;
}
