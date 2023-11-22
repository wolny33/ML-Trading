using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/investment")]
public sealed class TradeController : Controller
{
    /// <summary>
    ///     Starts/Stops the investment.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public InvestmentResponse StartStopTheInvestment(InvestmentRequest request)
    {
        return new InvestmentResponse
        {
            Enabled = request.Enable
        };
    }

    /// <summary>
    ///     Returns information if the investment is started or stopped.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public InvestmentResponse IsTheInvestmentOn()
    {
        return new InvestmentResponse
        {
            Enabled = false
        };
    }
}
