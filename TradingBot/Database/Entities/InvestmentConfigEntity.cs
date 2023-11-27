using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class InvestmentConfigEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required bool Enabled { get; set; }
}
