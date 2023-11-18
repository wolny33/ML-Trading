using Microsoft.AspNetCore.Mvc;
using TradingBot.Dto;
using TradingBot.Services;

namespace TradingBot.Controllers;

[ApiController]
[Route("api/settings/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly CredentialsCommand _command;

    public AuthController(CredentialsCommand command)
    {
        _command = command;
    }

    /// <summary>
    ///     Updates password
    /// </summary>
    /// <response code="204">Password was successfully changed</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdatePasswordAsync(ChangePasswordRequest request)
    {
        await _command.ChangePasswordAsync(User.GetUsername(), request.NewPassword);
        return NoContent();
    }
}
