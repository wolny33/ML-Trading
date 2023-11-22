using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class TestModeResponse
{
    [Required]
    public required bool Enabled { get; init; }
}
