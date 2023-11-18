using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers;

[Route("api/test-mode")]
[ApiController]
public sealed class TestModeController : ControllerBase
{
    /// <summary>
    ///     Turns the test mode on or off.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    public IActionResult TurnTestModeOnOff()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Returns information if the test mode is on or off.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    public IActionResult IsTestModeOn()
    {
        return Ok();
    }
}
