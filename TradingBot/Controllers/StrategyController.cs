using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[Route("api/strategy")]
[ApiController]
public sealed class StrategyController : ControllerBase
{
    private readonly IStrategyParametersService _strategyParametersService;

    public StrategyController(IStrategyParametersService strategyParametersService)
    {
        _strategyParametersService = strategyParametersService;
    }

    /// <summary>
    ///     Gets the strategy parameters
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(StrategyParametersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<StrategyParametersResponse> GetStrategyParameters()
    {
        return (await _strategyParametersService.GetConfigurationAsync(HttpContext.RequestAborted)).ToResponse();
    }

    /// <summary>
    ///     Changes the strategy parameters
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(typeof(StrategyParametersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<StrategyParametersResponse> ChangeStrategyParameters(StrategyParametersRequest request)
    {
        return (await _strategyParametersService.SetParametersAsync(request.MaxStocksBuyCount,
            request.MinDaysDecreasing, request.MinDaysIncreasing,
            request.TopGrowingSymbolsBuyRatio, HttpContext.RequestAborted)).ToResponse();
    }
}
