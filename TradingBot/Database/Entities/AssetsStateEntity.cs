using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TradingBot.Models;

namespace TradingBot.Database.Entities;

[Index(nameof(CreationTimestamp))]
public sealed class AssetsStateEntity
{
    [Key]
    public Guid Id { get; init; }

    public required long CreationTimestamp { get; init; }
    public required string MainCurrency { get; init; }
    public required double EquityValue { get; init; }
    public required double AvailableCash { get; init; }
    public required double BuyingPower { get; init; }
    public required Mode Mode { get; init; }

    [Required]
    public IList<PositionEntity> HeldPositions { get; init; } = new List<PositionEntity>();

    public Guid? BacktestId { get; init; }
    public BacktestEntity? Backtest { get; init; }
}
