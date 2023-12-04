using Alpaca.Markets;
using Microsoft.AspNetCore.Mvc;

namespace TradingBot.Exceptions.Alpaca;

public sealed class BadAlpacaRequestException : UnsuccessfulAlpacaResponseException
{
    private readonly RequestValidationException _exception;

    public BadAlpacaRequestException(RequestValidationException exception) : base("bad-alpaca-request",
        $"Validation failed for property '{exception.PropertyName}': {exception.Message}")
    {
        _exception = exception;
        StatusCode = StatusCodes.Status400BadRequest;
    }

    public override object ToResponse()
    {
        return new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [_exception.PropertyName] = new[] { _exception.Message }
        });
    }
}
