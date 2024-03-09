using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services.Strategy;

namespace TradingBot.Controllers;

[Route("api/strategy")]
[ApiController]
public sealed class StrategyController : ControllerBase
{
    private readonly IStrategyParametersService _strategyParametersService;
    private readonly IStrategySelectionService _strategySelectionService;

    public StrategyController(IStrategyParametersService strategyParametersService,
        IStrategySelectionService strategySelectionService)
    {
        _strategyParametersService = strategyParametersService;
        _strategySelectionService = strategySelectionService;
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
            request.MinDaysDecreasing, request.MinDaysIncreasing, request.TopGrowingSymbolsBuyRatio,
            HttpContext.RequestAborted)).ToResponse();
    }

    /// <summary>
    ///     Gets the name of selected strategy
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("selection")]
    [ProducesResponseType(typeof(StrategySelectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<StrategySelectionResponse> GetStrategySelection()
    {
        var name = await _strategySelectionService.GetSelectedNameAsync(HttpContext.RequestAborted);
        return new StrategySelectionResponse { Name = name };
    }

    /// <summary>
    ///     Changes selected strategy
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [Route("selection")]
    [ProducesResponseType(typeof(StrategySelectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<StrategySelectionResponse> ChangeStrategySelection(StrategySelectionRequest request)
    {
        await _strategySelectionService.SetNameAsync(request.Name, HttpContext.RequestAborted);
        return new StrategySelectionResponse { Name = request.Name };
    }

    /// <summary>
    ///     Gets known strategy names
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [Route("selection/names")]
    [ProducesResponseType(typeof(StrategyNamesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public StrategyNamesResponse GetStrategyNames()
    {
        return new StrategyNamesResponse { Names = StrategySelectionService.ValidNames };
    }
}
