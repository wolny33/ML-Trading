using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TradingBot.Models;

namespace TradingBot.Database.Entities;

[Index(nameof(ExecutionStartTimestamp))]
public sealed class BacktestEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required DateOnly SimulationStart { get; init; }
    public required DateOnly SimulationEnd { get; init; }
    public required long ExecutionStartTimestamp { get; init; }
    public long? ExecutionEndTimestamp { get; set; }
    public required BacktestState State { get; set; }
    public required string StateDetails { get; set; }
    public IList<TradingTaskEntity> TradingTasks { get; init; } = new List<TradingTaskEntity>();
    public IList<AssetsStateEntity> AssetsStates { get; init; } = new List<AssetsStateEntity>();
}
