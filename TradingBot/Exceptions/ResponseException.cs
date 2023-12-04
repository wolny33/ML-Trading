namespace TradingBot.Exceptions;

public abstract class ResponseException : Exception
{
    protected ResponseException(string code, string message, Exception? inner = null) : base(message, inner)
    {
        ErrorCode = code;
    }

    public int StatusCode { get; protected set; } = StatusCodes.Status500InternalServerError;
    public string ErrorCode { get; }

    public virtual ErrorResponse ToResponse()
    {
        return new ErrorResponse
        {
            Code = ErrorCode,
            Message = Message
        };
    }
}

public class ErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
}
