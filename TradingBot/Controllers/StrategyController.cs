using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;

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
    [ProducesResponseType(typeof(StrategySettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public StrategySettingsResponse GetStrategyParameters()
    {
        return new StrategySettingsResponse
        {
            ImportantProperty = "value"
        };
    }

    /// <summary>
    ///     Changes the strategy parameters.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(typeof(StrategySettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public StrategySettingsResponse ChangeStrategyParameters(StrategySettingsRequest request)
    {
        return new StrategySettingsResponse
        {
            ImportantProperty = request.ImportantProperty
        };
    }
}
