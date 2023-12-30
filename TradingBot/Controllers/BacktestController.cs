﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/backtests")]
public sealed class BacktestController : ControllerBase
{
    private readonly IBacktestExecutor _backtestExecutor;
    private readonly ISystemClock _clock;
    private readonly IBacktestQuery _query;

    public BacktestController(IBacktestQuery query, IBacktestExecutor backtestExecutor, ISystemClock clock)
    {
        _query = query;
        _backtestExecutor = backtestExecutor;
        _clock = clock;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BacktestResponse>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<BacktestResponse>> GetAllAsync([FromQuery] BacktestRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? _clock.UtcNow;
        var start = request.Start ?? end - TimeSpan.FromDays(10);

        return (await _query.GetAllAsync(start, end, HttpContext.RequestAborted)).Select(b => b.ToResponse()).ToList();
    }

    [HttpGet]
    [Route("{id:guid}")]
    [ProducesResponseType(typeof(BacktestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BacktestResponse>> GetByIdAsync(Guid id)
    {
        var backtest = await _query.GetByIdAsync(id, HttpContext.RequestAborted);
        if (backtest is null) return NotFound();

        return backtest.ToResponse();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult StartNew(BacktestCreationRequest request)
    {
        var id = _backtestExecutor.StartNew(new BacktestDetails(request.Start, request.End, request.InitialCash));
        return Accepted(new Uri(id.ToString(), UriKind.Relative));
    }

    [HttpDelete]
    [Route("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CancelAsync(Guid id)
    {
        await _backtestExecutor.CancelBacktestAsync(id);
        return NoContent();
    }

    [HttpGet]
    [Route("{id:guid}/trading-tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<TradingTaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TradingTaskResponse>>> GetTasksAsync(Guid id)
    {
        var tasks = await _query.GetTasksForBacktestAsync(id, HttpContext.RequestAborted);
        if (tasks is null) return NotFound();

        return tasks.Select(t => t.ToResponse()).ToList();
    }

    [HttpGet]
    [Route("{id:guid}/asset-states")]
    [ProducesResponseType(typeof(IReadOnlyList<AssetsStateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssetsStateResponse>>> GetAsseStatesAsync(Guid id)
    {
        var assets = await _query.GetAssetsStatesForBacktestAsync(id, HttpContext.RequestAborted);
        if (assets is null) return NotFound();

        return assets.Select(a => a.ToResponse()).ToList();
    }
}
