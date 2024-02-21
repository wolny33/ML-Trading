using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

[Route("api/performance")]
[ApiController]
public sealed class PerformanceController : ControllerBase
{
    private readonly ITradingActionQuery _actionsQuery;
    private readonly IAssetsStateQuery _assetsStateQuery;
    private readonly ISystemClock _clock;
    private readonly ITestModeConfigService _testModeConfig;

    public PerformanceController(ITradingActionQuery actionsQuery, ISystemClock clock,
        IAssetsStateQuery assetsStateQuery, ITestModeConfigService testModeConfig)
    {
        _actionsQuery = actionsQuery;
        _clock = clock;
        _assetsStateQuery = assetsStateQuery;
        _testModeConfig = testModeConfig;
    }

    /// <summary>
    ///     Gets information about profits and losses
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<ReturnResponse>> GetReturnsAsync([FromQuery] ReturnsRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? _clock.UtcNow;
        var start = request.Start ?? end - TimeSpan.FromDays(10);
        var mode = await _testModeConfig.GetCurrentModeAsync(HttpContext.RequestAborted);

        var initial = (await _assetsStateQuery.GetEarliestStateAsync(mode, HttpContext.RequestAborted))?.Assets
            .EquityValue;
        if (initial is null)
            // There are no saved asset states to return
            return Array.Empty<ReturnResponse>();

        var states = await _assetsStateQuery.GetStatesFromRangeAsync(start, end, mode, HttpContext.RequestAborted);
        return states.Select(s => new ReturnResponse
        {
            Return = initial.Value != 0 ? (s.Assets.EquityValue - initial.Value) / initial.Value : 0,
            Time = s.CreatedAt
        }).ToList();
    }

    /// <summary>
    ///     Gets list of trading actions taken
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("trading-actions")]
    [ProducesResponseType(typeof(IReadOnlyList<TradingActionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<TradingActionResponse>> GetTradingActionsAsync(
        [FromQuery] TradingActionRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? _clock.UtcNow;
        var start = request.Start ?? end - TimeSpan.FromDays(10);

        var actions = await _actionsQuery.GetTradingActionsAsync(start, end, HttpContext.RequestAborted);

        return actions.Select(a => a.ToResponse()).ToList();
    }

    /// <summary>
    ///     Gets trade action by ID
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Not found</response>
    [HttpGet]
    [Route("trading-actions/{id:guid}")]
    [ProducesResponseType(typeof(TradingActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradingActionResponse>> GetTradeActionsAsync([FromRoute] Guid id)
    {
        var result = await _actionsQuery.GetTradingActionByIdAsync(id, HttpContext.RequestAborted);
        return result is not null ? result.ToResponse() : NotFound();
    }
}
