using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/investment")]
public sealed class TradeController : Controller
{
    private readonly IInvestmentConfigService _investmentConfig;

    public TradeController(IInvestmentConfigService investmentConfig)
    {
        _investmentConfig = investmentConfig;
    }

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
    public async Task<InvestmentResponse> ToggleInvestmentAsync(InvestmentRequest request)
    {
        return (await _investmentConfig.SetEnabledAsync(request.Enable, HttpContext.RequestAborted)).ToResponse();
    }

    /// <summary>
    ///     Returns information if the investment is started or stopped.
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(InvestmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<InvestmentResponse> GetInvestmentConfigurationAsync()
    {
        return (await _investmentConfig.GetConfigurationAsync(HttpContext.RequestAborted)).ToResponse();
    }
}
