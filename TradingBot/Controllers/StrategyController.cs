using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StrategyController : ControllerBase
    {
        /// <summary>
        /// Gets the strategy parameters.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        public IActionResult GetStrategyParameters()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Changes the strategy parameters.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost]
        public IActionResult ChangeStrategyParameters()
        {
            throw new NotImplementedException();
        }
    }
}
