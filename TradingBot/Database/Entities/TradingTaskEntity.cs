using System.ComponentModel.DataAnnotations;
using TradingBot.Models;

namespace TradingBot.Database.Entities;

public sealed class TradingTaskEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required long StartTimestamp { get; init; }
    public required long? EndTimestamp { get; set; }
    public required TradingTaskState State { get; set; }
    public required string StateDetails { get; set; }
    public required Mode Mode { get; init; }

    [Required]
    public IList<TradingActionEntity> TradingActions { get; set; } = new List<TradingActionEntity>();

    public Guid? BacktestId { get; init; }
    public BacktestEntity? Backtest { get; init; }
}
