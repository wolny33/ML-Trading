using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/test-mode")]
public sealed class TestModeController : ControllerBase
{
    private readonly ITestModeConfigService _testModeConfig;

    public TestModeController(ITestModeConfigService testModeConfig)
    {
        _testModeConfig = testModeConfig;
    }

    /// <summary>
    ///     Turns the test mode on or off
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="400">Bad request</response>
    /// <response code="401">Unauthorized</response>
    [HttpPut]
    [ProducesResponseType(typeof(TestModeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<TestModeResponse> ToggleTestModeAsync(TestModeRequest request)
    {
        return (await _testModeConfig.SetEnabledAsync(request.Enable, HttpContext.RequestAborted)).ToResponse();
    }

    /// <summary>
    ///     Returns information if the test mode is on or off
    /// </summary>
    /// <response code="200">OK</response>
    /// <response code="401">Unauthorized</response>
    [HttpGet]
    [ProducesResponseType(typeof(TestModeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<TestModeResponse> GetTestModeConfigurationAsync()
    {
        return (await _testModeConfig.GetConfigurationAsync(HttpContext.RequestAborted)).ToResponse();
    }
}
