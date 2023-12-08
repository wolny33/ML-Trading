using System.ComponentModel.DataAnnotations;

namespace TradingBot.Configuration;

public sealed class SeedCredentialsConfiguration
{
    public const string SectionName = "SeedCredentials";

    [Required]
    public required string DefaultUsername { get; init; }

    [Required]
    public required string DefaultPassword { get; init; }
}
