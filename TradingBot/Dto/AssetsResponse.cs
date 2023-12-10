using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class AssetsResponse
{
    [Required]
    public required decimal EquityValue { get; init; }

    [Required]
    public required CashResponse Cash { get; init; }

    [Required]
    public required IReadOnlyList<PositionResponse> Positions { get; init; }
}

public sealed class PositionResponse
{
    [Required]
    public required string Symbol { get; init; }

    [Required]
    public required decimal Quantity { get; init; }

    [Required]
    public required decimal AvailableQuantity { get; init; }

    [Required]
    public required decimal MarketValue { get; init; }

    [Required]
    public required decimal AverageEntryPrice { get; init; }
}

public sealed class CashResponse
{
    [Required]
    public required string MainCurrency { get; init; }

    [Required]
    public required decimal AvailableAmount { get; init; }
    [Required]
    public required decimal BuyingPower { get; init; }
}
