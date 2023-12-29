using System.Text.RegularExpressions;
using Alpaca.Markets;

namespace TradingBot.Exceptions.Alpaca;

public sealed partial class InsufficientAssetsException : UnsuccessfulAlpacaResponseException
{
    public InsufficientAssetsException() : base("insufficient-assets",
        "Requested asset amount in sell order is greater than available amount")
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity;
    }

    public static InsufficientAssetsException? FromWrapperException(RestClientErrorException exception)
    {
        if (exception.ErrorCode != 40310000 || !InsufficientAssetsRegex().IsMatch(exception.Message)) return null;

        return new InsufficientAssetsException();
    }

    [GeneratedRegex("^account is not allowed to short")]
    private static partial Regex InsufficientAssetsRegex();
}
