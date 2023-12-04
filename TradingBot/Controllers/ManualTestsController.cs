﻿using System.ComponentModel.DataAnnotations;
using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Models;
using TradingBot.Services;
using OrderType = TradingBot.Models.OrderType;

namespace TradingBot.Controllers;

/// <summary>
///     Temporary endpoints for manual testing
/// </summary>
[ApiController]
[Route("manual-tests")]
public sealed class ManualTestsController : ControllerBase
{
    private readonly ITradingActionQuery _actionQuery;
    private readonly IMarketDataSource _dataSource;
    private readonly IActionExecutor _executor;
    private readonly IPricePredictor _predictor;

    public ManualTestsController(IPricePredictor predictor, IMarketDataSource dataSource, IActionExecutor executor,
        ITradingActionQuery actionQuery)
    {
        _predictor = predictor;
        _dataSource = dataSource;
        _executor = executor;
        _actionQuery = actionQuery;
    }

    [HttpGet]
    [Route("predict")]
    [ProducesResponseType(typeof(IDictionary<string, Prediction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IDictionary<string, Prediction>> MakePredictionsAsync()
    {
        return (await _predictor.GetPredictionsAsync(HttpContext.RequestAborted)).ToDictionary(p => p.Key.Value,
            p => p.Value);
    }

    [HttpGet]
    [Route("market-data")]
    [ProducesResponseType(typeof(IDictionary<string, IReadOnlyList<DailyTradingData>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IDictionary<string, IReadOnlyList<DailyTradingData>>> GetAllDataAsync()
    {
        return (await _dataSource.GetPricesAsync(DateOnly.FromDateTime(DateTime.Now).AddDays(-10),
            DateOnly.FromDateTime(DateTime.Now),
            HttpContext.RequestAborted)).ToDictionary(p => p.Key.Value, p => p.Value);
    }

    [HttpGet]
    [Route("market-data/{symbol}")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyTradingData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DailyTradingData>>> GetDataAsync([FromRoute] string symbol)
    {
        var result = await _dataSource.GetDataForSingleSymbolAsync(new TradingSymbol(symbol),
            DateOnly.FromDateTime(DateTime.Now).AddDays(-10), DateOnly.FromDateTime(DateTime.Now),
            HttpContext.RequestAborted);

        if (result is null) return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Route("trading-actions")]
    [ProducesResponseType(typeof(TradingActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradingActionResponse>> ExecuteActionAsync(TradingActionRequest request)
    {
        var action = request.ToTradingAction();
        await _executor.ExecuteActionAsync(action, HttpContext.RequestAborted);
        var result = await _actionQuery.GetTradingActionByIdAsync(action.Id, HttpContext.RequestAborted);
        return result is not null ? result.ToResponse() : NotFound();
    }
}

public sealed class TradingActionRequest : IValidatableObject
{
    public decimal? Price { get; init; }

    [Required]
    public required decimal Quantity { get; init; }

    [Required]
    public required string Symbol { get; init; }

    [Required]
    public required string InForce { get; init; }

    [Required]
    public required string OrderType { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.TryParse<TimeInForce>(InForce, true, out _))
            yield return new ValidationResult($"'{InForce}' is not a valid order duration", new[] { nameof(InForce) });

        if (!Enum.TryParse<OrderType>(OrderType, true, out _))
            yield return new ValidationResult($"'{OrderType}' is not a valid order type", new[] { nameof(OrderType) });
    }

    public TradingAction ToTradingAction()
    {
        return new TradingAction
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.Now,
            Symbol = new TradingSymbol(Symbol),
            Quantity = Quantity,
            Price = Price,
            OrderType = Enum.Parse<OrderType>(OrderType),
            InForce = Enum.Parse<TimeInForce>(InForce)
        };
    }
}
