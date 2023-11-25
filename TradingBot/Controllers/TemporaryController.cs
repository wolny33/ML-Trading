using Microsoft.AspNetCore.Mvc;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("temporary")]
public sealed class TemporaryController : ControllerBase
{
    [HttpPost]
    [Route("predict")]
    public async Task<Prediction> MakePredictionAsync(IReadOnlyList<DailyTradingData> request)
    {
        return await PricePredictor.PredictForSymbolAsync(request);
    }
}
