using System.Text.RegularExpressions;
using Alpaca.Markets;

namespace TradingBot.Exceptions.Alpaca;

public sealed partial class InvalidFractionalOrderException : UnsuccessfulAlpacaResponseException
{
    public InvalidFractionalOrderException() : base("invalid-fractional-order",
        "Fractional orders must be 'Market' orders and have duration 'Day'")
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity;
    }

    public static InvalidFractionalOrderException? FromWrapperException(RestClientErrorException exception)
    {
        if (exception.ErrorCode != 42210000 || !InvalidFractionalOrderRegex().IsMatch(exception.Message)) return null;

        return new InvalidFractionalOrderException();
    }

    [GeneratedRegex("^fractional orders must be")]
    private static partial Regex InvalidFractionalOrderRegex();
}
