using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/investment")]
public sealed class TradeController : Controller
{
    /// <summary>
    ///     Starts/Stops the investment.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    public IActionResult StartStopTheInvestment()
    {
        //return Ok();
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns information if the investment is started or stopped.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    public IActionResult IsTheInvestmentOn()
    {
        //return Ok(false);
        throw new NotImplementedException();
    }
}
