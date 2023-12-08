namespace TradingBot.Exceptions;

public abstract class ApiCallException : ResponseException
{
    protected ApiCallException(string code, string message, Exception? inner = null) : base(code, message, inner)
    {
        StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
}
