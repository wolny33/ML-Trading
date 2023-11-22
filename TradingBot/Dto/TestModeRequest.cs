using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class TestModeRequest
{
    [Required]
    public required bool Enable { get; init; }
}
