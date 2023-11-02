using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestModeController : ControllerBase
    {
        /// <summary>
        /// Turns the test mode on or off.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost()]
        public IActionResult TurnTestModeOnOff()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Returns information if the test mode is on or off.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet()]
        public IActionResult IsTestModeOn()
        {
            //return Ok(true);
            throw new NotImplementedException();
        }
    }
}
