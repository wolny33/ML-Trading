using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace TradingBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PerformenceController : ControllerBase
    {
        /// <summary>
        /// Gets information about profits and losses.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet]
        public ActionResult CheckPerformence()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Gets list of trade actions taken.
        /// </summary>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("tradeactions")]
        public ActionResult GetTradeActions()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Gets details of trade action.
        /// </summary>
        /// <param name="id">Id of the trade action which details should be displayed.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad request</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("details/{id}")]
        public ActionResult GetTradeActionDetails([FromRoute(Name = "id")][Required] Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
