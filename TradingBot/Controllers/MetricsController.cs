using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/metrics")]
public sealed class MetricsController : ControllerBase
{
    private readonly IMarketDataCache _marketDataCache;

    public MetricsController(IMarketDataCache marketDataCache)
    {
        _marketDataCache = marketDataCache;
    }

    /// <summary>
    ///     Returns current statistics of memory cache used to store market data
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("cache")]
    [ProducesResponseType(typeof(MemoryCacheStatistics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public MemoryCacheStatistics GetCacheStats()
    {
        return _marketDataCache.GetCacheStats();
    }
}
