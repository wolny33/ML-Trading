using System.ComponentModel.DataAnnotations;

namespace TradingBot.Dto;

public sealed class AssetsResponse
{
    /// <summary>
    ///     Total value of all owned assets
    /// </summary>
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

    /// <summary>
    ///     Total quantity owned
    /// </summary>
    [Required]
    public required decimal Quantity { get; init; }

    /// <summary>
    ///     Quantity available for trading (not reserved due to active orders)
    /// </summary>
    [Required]
    public required decimal AvailableQuantity { get; init; }

    /// <summary>
    ///     Current value of this asset
    /// </summary>
    [Required]
    public required decimal MarketValue { get; init; }

    /// <summary>
    ///     Average price at which this asset was bought
    /// </summary>
    [Required]
    public required decimal AverageEntryPrice { get; init; }
}

public sealed class CashResponse
{
    [Required]
    public required string MainCurrency { get; init; }

    /// <summary>
    ///     Currently owned amount of money
    /// </summary>
    [Required]
    public required decimal AvailableAmount { get; init; }

    /// <summary>
    ///     Amount of money available for trading (not reserved due to active orders)
    /// </summary>
    [Required]
    public required decimal BuyingPower { get; init; }
}
