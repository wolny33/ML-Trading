using System.Text.RegularExpressions;
using Alpaca.Markets;

namespace TradingBot.Exceptions.Alpaca;

public sealed partial class InsufficientFundsException : UnsuccessfulAlpacaResponseException
{
    public InsufficientFundsException() : base("insufficient-funds",
        "Not enough buying power to execute requested order")
    {
        StatusCode = StatusCodes.Status422UnprocessableEntity;
    }

    public static InsufficientFundsException? FromWrapperException(RestClientErrorException exception)
    {
        if (exception.ErrorCode != 40310000 || !InsufficientFundsRegex().IsMatch(exception.Message)) return null;

        return new InsufficientFundsException();
    }

    [GeneratedRegex("^insufficient buying power")]
    private static partial Regex InsufficientFundsRegex();
}
