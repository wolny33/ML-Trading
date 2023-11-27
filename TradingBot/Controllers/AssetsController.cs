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
    public async Task<AssetsResponse> GetAsync()
    {
        return (await _assetsDataSource.GetAssetsAsync(HttpContext.RequestAborted)).ToResponse();
    }
}
