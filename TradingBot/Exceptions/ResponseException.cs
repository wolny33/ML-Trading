using System.ComponentModel.DataAnnotations;

namespace TradingBot.Exceptions;

public abstract class ResponseException : Exception
{
    protected ResponseException(string code, string message, Exception? inner = null) : base(message, inner)
    {
        ErrorCode = code;
    }

    public int StatusCode { get; protected set; } = StatusCodes.Status500InternalServerError;
    public string ErrorCode { get; }

    public virtual object ToResponse()
    {
        return GetError().ToResponse();
    }

    public Error GetError()
    {
        return new Error(ErrorCode, Message);
    }
}

public sealed record Error(string Code, string Message)
{
    public ErrorResponse ToResponse()
    {
        return new ErrorResponse
        {
            Code = Code,
            Message = Message
        };
    }
}

public sealed class ErrorResponse
{
    [Required]
    public required string Code { get; init; }

    [Required]
    public required string Message { get; init; }
}
