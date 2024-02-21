using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Models;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly IAssetsDataSource _assetsDataSource;
    private readonly ITestModeConfigService _testModeConfig;

    public AssetsController(IAssetsDataSource assetsDataSource, ITestModeConfigService testModeConfig)
    {
        _assetsDataSource = assetsDataSource;
        _testModeConfig = testModeConfig;
    }

    /// <summary>
    ///     Gets currently held assets, or last recorded assets state if Alpaca API is not available at the moment
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetsResponse>> GetAsync()
    {
        var mode = (await _testModeConfig.GetConfigurationAsync(HttpContext.RequestAborted)).Enabled
            ? Mode.TestMode
            : Mode.LiveTrading;

        var assets = await _assetsDataSource.GetLatestAssetsAsync(mode, HttpContext.RequestAborted);

        if (assets is not null) return assets.ToResponse();

        // TODO
        return NotFound();
    }
}
