using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[Route("api/performance")]
[ApiController]
public sealed class PerformanceController : ControllerBase
{
    private readonly ITradingActionQuery _actionsQuery;
    private readonly IAssetsStateQuery _assetsStateQuery;
    private readonly ISystemClock _clock;

    public PerformanceController(ITradingActionQuery actionsQuery, ISystemClock clock,
        IAssetsStateQuery assetsStateQuery)
    {
        _actionsQuery = actionsQuery;
        _clock = clock;
        _assetsStateQuery = assetsStateQuery;
    }

    /// <summary>
    ///     Gets information about profits and losses.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReturnsRequest>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<ReturnResponse>> GetReturnsAsync([FromQuery] ReturnsRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? _clock.UtcNow;
        var start = request.Start ?? end - TimeSpan.FromDays(10);

        var initial = (await _assetsStateQuery.GetEarliestStateAsync(HttpContext.RequestAborted))?.Assets.EquityValue;
        if (initial is null)
            // There are no saved asset states to return
            return Array.Empty<ReturnResponse>();

        var states = await _assetsStateQuery.GetStatesFromRangeAsync(start, end, HttpContext.RequestAborted);
        return states.Select(s => new ReturnResponse
        {
            Return = initial.Value != 0 ? (s.Assets.EquityValue - initial.Value) / initial.Value : 0,
            Time = s.CreatedAt
        }).ToList();
    }

    /// <summary>
    ///     Gets list of trade actions taken.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("trade-actions")]
    [ProducesResponseType(typeof(IReadOnlyList<TradingActionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<TradingActionResponse>> GetTradeActionsAsync(
        [FromQuery] TradingActionCollectionRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? _clock.UtcNow;
        var start = request.Start ?? end - TimeSpan.FromDays(10);

        var actions = request.Mocked
            ? _actionsQuery.CreateMockedTradingActions(start, end)
            : await _actionsQuery.GetTradingActionsAsync(start, end, HttpContext.RequestAborted);

        return actions.Select(a => a.ToResponse()).ToList();
    }

    /// <summary>
    ///     Gets trade action by ID.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Not found</response>
    [HttpGet]
    [Route("trade-actions/{id:guid}")]
    [ProducesResponseType(typeof(TradingActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradingActionResponse>> GetTradeActionsAsync([FromRoute] Guid id)
    {
        var result = await _actionsQuery.GetTradingActionByIdAsync(id, HttpContext.RequestAborted);
        return result is not null ? result.ToResponse() : NotFound();
    }

    /// <summary>
    ///     Gets details of trade action.
    /// </summary>
    /// <param name="id">ID of the trade action which details should be displayed.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    // TODO: Remove after updating UI
    [HttpGet]
    [Route("trade-actions/{id:guid}/details")]
    [ProducesResponseType(typeof(TradingActionDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public TradingActionDetailsResponse GetTradeActionDetails([Required] Guid id)
    {
        return new TradingActionDetailsResponse { Id = id };
    }
}
