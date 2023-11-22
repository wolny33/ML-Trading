using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers;

[Route("api/strategy")]
[ApiController]
public sealed class StrategyController : ControllerBase
{
    /// <summary>
    ///     Gets the strategy parameters.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetStrategyParameters()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Changes the strategy parameters.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ChangeStrategyParameters()
    {
        throw new NotImplementedException();
    }
}
