using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Exceptions.Alpaca;

public sealed class BadAlpacaRequestException : UnsuccessfulAlpacaResponseException
{
    private readonly string _message;
    private readonly string _propertyName;

    public BadAlpacaRequestException(RequestValidationException exception) : this(exception.PropertyName,
        exception.Message)
    {
    }

    public BadAlpacaRequestException(string propertyName, string message) : base("bad-alpaca-request",
        $"Validation failed for property '{propertyName}': {message}")
    {
        _propertyName = propertyName;
        _message = message;
        StatusCode = StatusCodes.Status400BadRequest;
    }

    public override object ToResponse()
    {
        return new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [_propertyName] = new[] { _message }
        });
    }
}
