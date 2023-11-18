using System.Security.Claims;

namespace TradingBot;

public static class ClaimsExtensions
{
    public static string GetUsername(this ClaimsPrincipal user)
    {
        var username = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        return username ?? throw new InvalidOperationException("User does not have a name claim");
    }
}
