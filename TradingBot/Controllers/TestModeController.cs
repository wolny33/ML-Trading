using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/test-mode")]
public sealed class TestModeController : ControllerBase
{
    /// <summary>
    ///     Turns the test mode on or off.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(typeof(TestModeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public TestModeResponse TurnTestModeOnOff(TestModeRequest request)
    {
        return new TestModeResponse
        {
            Enabled = request.Enable
        };
    }

    /// <summary>
    ///     Returns information if the test mode is on or off.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(TestModeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public TestModeResponse IsTestModeOn()
    {
        return new TestModeResponse
        {
            Enabled = true
        };
    }
}
