using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TradingBot.Services;

namespace TradingBot;

public sealed class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "BasicAuthentication";

    private readonly CredentialsCommand _command;

    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock, CredentialsCommand command) : base(options,
        logger, encoder, clock)
    {
        _command = command;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.Fail("Missing Authorization Header");

        var username = await ValidateAndGetUsernameAsync(Request.Headers["Authorization"]);
        if (username is null) return AuthenticateResult.Fail("Invalid credentials");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<string?> ValidateAndGetUsernameAsync(StringValues header)
    {
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(header);
            if (authHeader.Parameter is null) return null;

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            var username = credentials[0];
            var password = credentials[1];

            return await _command.AreCredentialsValidAsync(username, password) ? username : null;
        }
        catch
        {
            return null;
        }
    }
}
