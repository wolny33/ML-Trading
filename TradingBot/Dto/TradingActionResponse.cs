using System.ComponentModel.DataAnnotations;
using TradingBot.Exceptions;

namespace TradingBot.Dto;

public sealed class TradingActionResponse
{
    [Required]
    public required Guid Id { get; init; }

    [Required]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///     Requested price for limit orders, or <c>null</c> for market orders
    /// </summary>
    [Required]
    public required decimal? Price { get; init; }

    [Required]
    public required decimal Quantity { get; init; }

    [Required]
    public required string Symbol { get; init; }

    /// <summary>
    ///     Order's expiration behavior
    /// </summary>
    [Required]
    public required string InForce { get; init; }

    [Required]
    public required string OrderType { get; init; }

    [Required]
    public required string Status { get; set; }

    /// <summary>
    ///     Time at which this action was completed, either by being filled, cancelled, rejected or due to an error
    /// </summary>
    public required DateTimeOffset? ExecutedAt { get; init; }

    public required Guid? AlpacaId { get; init; }

    /// <summary>
    ///     Average price at which this order was filled (<c>null</c> if it was not filled yet)
    /// </summary>
    public required decimal? AverageFillPrice { get; init; }

    /// <summary>
    ///     Error that occured when posting this action, or <c>null</c> if everything completed correctly
    /// </summary>
    public required ErrorResponse? Error { get; init; }

    /// <summary>
    ///     ID of trading task during which this trading action was executed
    /// </summary>
    public required Guid? TaskId { get; init; }
}
