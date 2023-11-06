using System.ComponentModel.DataAnnotations;

namespace TradingBot.Configuration;

public sealed class AlpacaConfiguration
{
    public const string SectionName = "AlpacaApi";

    [Required]
    public required string Key { get; init; }

    [Required]
    public required string Secret { get; init; }
}
