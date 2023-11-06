using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers;

[Route("api/performance")]
[ApiController]
public sealed class PerformanceController : ControllerBase
{
    /// <summary>
    ///     Gets information about profits and losses.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    public ActionResult CheckPerformance()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets list of trade actions taken.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("trade-actions")]
    public ActionResult GetTradeActions()
    {
        throw new NotImplementedException();
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
    public ActionResult GetTradeActionDetails([Required] Guid id)
    {
        throw new NotImplementedException();
    }
}
