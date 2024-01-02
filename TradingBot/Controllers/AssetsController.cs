using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/assets")]
public class AssetsController : ControllerBase
{
    private readonly IAssetsDataSource _assetsDataSource;

    public AssetsController(IAssetsDataSource assetsDataSource)
    {
        _assetsDataSource = assetsDataSource;
    }

    /// <summary>
    ///     Gets currently held assets
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetsResponse>> GetAsync([FromQuery] bool mocked = false)
    {
        var assets = mocked
            ? await _assetsDataSource.GetMockedAssetsAsync(HttpContext.RequestAborted)
            : await _assetsDataSource.GetLatestAssetsAsync(HttpContext.RequestAborted);

        if (assets is not null) return assets.ToResponse();

        return NotFound();
    }
}
