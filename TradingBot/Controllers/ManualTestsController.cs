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
    private readonly IPricePredictor _predictor;
    private readonly IMarketDataSource _dataSource;

    public ManualTestsController(IPricePredictor predictor, IMarketDataSource dataSource)
    {
        _predictor = predictor;
        _dataSource = dataSource;
    }

    [HttpGet]
    [Route("predict")]
    [ProducesResponseType(typeof(IDictionary<string, Prediction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IDictionary<string, Prediction>> MakePredictionAsync()
    {
        return (await _predictor.GetPredictionsAsync(HttpContext.RequestAborted)).ToDictionary(p => p.Key.Value,
            p => p.Value);
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

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }
}
