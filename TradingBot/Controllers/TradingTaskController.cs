using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/trading-tasks")]
public sealed class TradingTaskController : ControllerBase
{
    private readonly ITradingActionQuery _actionQuery;
    private readonly ISystemClock _clock;
    private readonly ITradingTaskQuery _query;

    public TradingTaskController(ISystemClock clock, ITradingTaskQuery query, ITradingActionQuery actionQuery)
    {
        _clock = clock;
        _query = query;
        _actionQuery = actionQuery;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TradingTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<TradingTaskResponse>> GetTradingTasksAsync([FromQuery] TradingTaskRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? _clock.UtcNow;
        var start = request.Start ?? end - TimeSpan.FromDays(10);

        var tasks = await _query.GetTradingTasksAsync(start, end, HttpContext.RequestAborted);
        return tasks.Select(t => t.ToResponse()).ToList();
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(TradingTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TradingTaskResponse>> GetTradingTaskByIdAsync([FromRoute] Guid id)
    {
        var task = await _query.GetTradingTaskByIdAsync(id, HttpContext.RequestAborted);
        return task is null ? NotFound() : task.ToResponse();
    }

    [HttpGet]
    [Route("{id:guid}/trading-actions")]
    [ProducesResponseType(typeof(IReadOnlyList<TradingActionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TradingActionResponse>>> GetActionsForTaskWithIdAsync(
        [FromRoute] Guid id)
    {
        var actions = await _actionQuery.GetActionsForTaskWithIdAsync(id, HttpContext.RequestAborted);
        return actions is null ? NotFound() : actions.Select(a => a.ToResponse()).ToList();
    }
}
