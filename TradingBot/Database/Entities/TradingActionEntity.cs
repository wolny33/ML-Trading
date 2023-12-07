using System.ComponentModel.DataAnnotations;
using Alpaca.Markets;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Database.Entities;

public sealed class TradingActionEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required long CreationTimestamp { get; init; }
    public required double? Price { get; init; }
    public required double Quantity { get; init; }
    public required string Symbol { get; init; }
    public required TimeInForce InForce { get; init; }
    public required OrderType OrderType { get; init; }
    public OrderStatus? Status { get; set; }
    public long? ExecutionTimestamp { get; set; }
    public Guid? AlpacaId { get; set; }
    public double? AverageFillPrice { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? TaskId { get; set; }
    public TradingTaskEntity? TradingTask { get; set; }
}
