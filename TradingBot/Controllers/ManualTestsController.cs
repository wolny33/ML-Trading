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

    public ManualTestsController(IPricePredictor predictor)
    {
        _predictor = predictor;
    }

    [HttpPost]
    [Route("predict")]
    [ProducesResponseType(typeof(Prediction), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<Prediction> MakePredictionAsync(IReadOnlyList<DailyTradingData> request)
    {
        return await _predictor.PredictForSymbolAsync(request, HttpContext.RequestAborted);
    }
}
