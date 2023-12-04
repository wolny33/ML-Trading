using System.Text.RegularExpressions;
using Alpaca.Markets;

namespace TradingBot.Exceptions.Alpaca;

public sealed partial class AssetNotFoundException : UnsuccessfulAlpacaResponseException
{
    private AssetNotFoundException(RestClientErrorException exception) : base("asset-not-found", exception.Message)
    {
        StatusCode = StatusCodes.Status404NotFound;
    }

    public static AssetNotFoundException? FromWrapperException(RestClientErrorException exception)
    {
        if (exception.ErrorCode != 42210000 || !AssetNotFoundRegex().IsMatch(exception.Message)) return null;

        return new AssetNotFoundException(exception);
    }

    [GeneratedRegex("^asset .* not found")]
    private static partial Regex AssetNotFoundRegex();
}
