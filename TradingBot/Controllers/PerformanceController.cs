using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[Route("api/performance")]
[ApiController]
public sealed class PerformanceController : ControllerBase
{
    private readonly ITradingActionQuery _query;

    public PerformanceController(ITradingActionQuery query)
    {
        _query = query;
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
    public IReadOnlyList<ReturnResponse> CheckPerformance([FromQuery] ReturnsRequest request)
    {
        var start = request.Start ?? DateTimeOffset.Now - TimeSpan.FromDays(10);
        var end = request.End ?? DateTimeOffset.Now;

        var first = start - start.TimeOfDay + TimeSpan.FromDays(1);
        return Enumerable.Range(0, (int)(end - first).TotalDays + 1).Select(i =>
            (Time: first + TimeSpan.FromDays(i), DailyChange: Random.Shared.NextDouble() * 0.6 - 0.3)).Aggregate(
            new List<ReturnResponse>(),
            (returns, change) =>
            {
                returns.Add(new ReturnResponse
                {
                    Return = ((returns.LastOrDefault()?.Return ?? 0) + 1) * (change.DailyChange + 1) - 1,
                    Time = change.Time
                });
                return returns;
            });
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
        [FromQuery] TradingActionRequest request)
    {
        var end = request.End ?? request.Start + TimeSpan.FromDays(10) ?? DateTimeOffset.Now;
        var start = request.Start ?? end - TimeSpan.FromDays(10);

        return (await _query.GetTradingActionsAsync(start, end, HttpContext.RequestAborted)).Select(a => a.ToResponse())
            .ToList();
    }

    /// <summary>
    ///     Gets details of trade action.
    /// </summary>
    /// <param name="id">Id of the trade action which details should be displayed.</param>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("trade-actions/{id:guid}")]
    [ProducesResponseType(typeof(TradingActionDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<TradingActionDetailsResponse> GetTradeActionDetailsAsync([Required] Guid id)
    {
        return (await _query.GetDetailsAsync(id, HttpContext.RequestAborted)).ToResponse();
    }
}
