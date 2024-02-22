using Microsoft.AspNetCore.Mvc;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

/// <summary>
///     Temporary endpoints for manual testing
/// </summary>
[ApiController]
[Route("manual-tests")]
public sealed class ManualTestsController : ControllerBase
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly IAssetsStateCommand _assetsStateCommand;
    private readonly IMarketDataSource _dataSource;
    private readonly IPricePredictor _predictor;
    private readonly TradingTaskExecutor _taskExecutor;
    private readonly ITestModeConfigService _testModeConfig;

    public ManualTestsController(IPricePredictor predictor, IMarketDataSource dataSource,
        TradingTaskExecutor taskExecutor, IAssetsStateCommand assetsStateCommand,
        IAssetsDataSource assetsDataSource, ITestModeConfigService testModeConfig)
    {
        _predictor = predictor;
        _dataSource = dataSource;
        _taskExecutor = taskExecutor;
        _assetsDataSource = assetsDataSource;
        _testModeConfig = testModeConfig;
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IDictionary<string, IReadOnlyList<DailyTradingData>>> GetAllDataAsync()
    {
        return (await _dataSource.GetPricesAsync(DateOnly.FromDateTime(DateTime.Now).AddDays(-10),
            DateOnly.FromDateTime(DateTime.Now),
            HttpContext.RequestAborted)).ToDictionary(p => p.Key.Value, p => p.Value);
    }

    [HttpGet]
    [Route("market-data/{symbol}")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyTradingData>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DailyTradingData>>> GetDataAsync([FromRoute] string symbol)
    {
        var result = await _dataSource.GetDataForSingleSymbolAsync(new TradingSymbol(symbol),
            DateOnly.FromDateTime(DateTime.Now).AddDays(-10), DateOnly.FromDateTime(DateTime.Now),
            HttpContext.RequestAborted);

        if (result is null) return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [Route("trading-tasks")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> ExecuteTaskAsync()
    {
        await _taskExecutor.ExecuteAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost]
    [Route("assets-states")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SaveCurrentAssetsStateAsync()
    {
        var assets = await _assetsDataSource.GetCurrentAssetsAsync(HttpContext.RequestAborted);
        var mode = await _testModeConfig.GetCurrentModeAsync(HttpContext.RequestAborted);
        await _assetsStateCommand.SaveCurrentAssetsAsync(assets, mode, HttpContext.RequestAborted);
        return NoContent();
    }
}
