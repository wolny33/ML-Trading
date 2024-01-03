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
    ///     Gets currently held assets, or last recorded assets state if Alpaca API is not available at the moment
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AssetsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetsResponse>> GetAsync()
    {
        var assets = await _assetsDataSource.GetLatestAssetsAsync(HttpContext.RequestAborted);

        if (assets is not null) return assets.ToResponse();

        // TODO
        return NotFound();
    }
}
