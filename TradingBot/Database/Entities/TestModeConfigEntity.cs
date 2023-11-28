using System.ComponentModel.DataAnnotations;

namespace TradingBot.Database.Entities;

public sealed class TestModeConfigEntity
{
    [Key]
    public required Guid Id { get; init; }

    public required bool Enabled { get; set; }
}
