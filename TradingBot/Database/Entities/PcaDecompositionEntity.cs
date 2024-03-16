using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TradingBot.Database.Entities;

[Index(nameof(CreationTimestamp))]
public sealed class PcaDecompositionEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required Guid BacktestId { get; init; }
    public required long CreationTimestamp { get; init; }
    public required DateOnly CreatedAt { get; init; }
    public required DateOnly ExpiresAt { get; init; }
    public required string Symbols { get; init; }
    public required string Means { get; init; }
    public required string StandardDeviations { get; init; }
    public required string PrincipalVectors { get; init; }
}
