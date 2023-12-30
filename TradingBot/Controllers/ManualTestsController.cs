using System.ComponentModel.DataAnnotations;
using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IAssetsStateCommand _assetsStateCommand;
    private readonly IMarketDataSource _dataSource;
    private readonly IActionExecutor _executor;
    private readonly IMemoryCache _memoryCache;
    private readonly IPricePredictor _predictor;
    private readonly IStrategy _strategy;
    private readonly TradingTaskExecutor _taskExecutor;

    public ManualTestsController(IPricePredictor predictor, IMarketDataSource dataSource, IActionExecutor executor,
        ITradingActionQuery actionQuery, TradingTaskExecutor taskExecutor, IAssetsStateCommand assetsStateCommand,
        IStrategy strategy, IMemoryCache memoryCache, IAssetsDataSource assetsDataSource)
    {
        _predictor = predictor;
        _dataSource = dataSource;
        _executor = executor;
        _actionQuery = actionQuery;
        _taskExecutor = taskExecutor;
        _strategy = strategy;
        _memoryCache = memoryCache;
        _assetsDataSource = assetsDataSource;
        _assetsStateCommand = assetsStateCommand;
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

    [HttpPost]
    [Route("trading-tasks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ExecuteTaskAsync()
    {
        await _taskExecutor.ExecuteAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpGet]
    [Route("strategy-results")]
    [ProducesResponseType(typeof(IReadOnlyList<TradingAction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TradingAction>>> GetStrategyResultsAsync()
    {
        var result = await _strategy.GetTradingActionsAsync(HttpContext.RequestAborted);

        return Ok(result);
    }

    [HttpPost]
    [Route("assets-states")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SaveCurrentAssetsStateAsync()
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(HttpContext.RequestAborted);
        await _assetsStateCommand.SaveCurrentAssetsAsync(assets, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpGet]
    [Route("cache-stats")]
    [ProducesResponseType(typeof(MemoryCacheStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<MemoryCacheStatistics> GetCacheStats()
    {
        if (_memoryCache.GetCurrentStatistics() is { } stats) return stats;
        return NotFound();
    }

    [HttpPost]
    [Route("init-backtest-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> InitCacheAsync([FromQuery][Required] DateOnly start,
        [FromQuery][Required] int symbols)
    {
        await _dataSource.InitializeBacktestDataAsync(start, DateOnly.FromDateTime(DateTime.Now).AddDays(-1), symbols);
        return NoContent();
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

    public Guid? TaskId { get; init; }

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
            InForce = Enum.Parse<TimeInForce>(InForce),
            TaskId = TaskId
        };
    }
}
