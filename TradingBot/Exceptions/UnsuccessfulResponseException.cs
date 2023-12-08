// ReSharper disable VirtualMemberCallInConstructor

using System.Net;

namespace TradingBot.Exceptions;

public class UnsuccessfulResponseException : ApiCallException
{
    protected UnsuccessfulResponseException(string apiName, int statusCode, string body) : base(
        "unsuccessful-api-response", $"{apiName} responded with status code {statusCode}: {body}")
    {
    }

    protected UnsuccessfulResponseException(string code, string message) : base(code, message)
    {
    }
}

public class UnsuccessfulPredictorResponseException : UnsuccessfulResponseException
{
    public UnsuccessfulPredictorResponseException(int statusCode, string body) : base("Predictor service", statusCode,
        body)
    {
        Data["StatusCode"] = statusCode;
        Data["Response"] = body;
    }

    protected UnsuccessfulPredictorResponseException(string code, string message) : base(code, message)
    {
    }
}

public class UnsuccessfulAlpacaResponseException : UnsuccessfulResponseException
{
    public UnsuccessfulAlpacaResponseException(int statusCode, string body) : base("Alpaca API", statusCode, body)
    {
        Data["StatusCode"] = statusCode;
        Data["Response"] = body;
    }

    public UnsuccessfulAlpacaResponseException(HttpStatusCode statusCode, int errorCode, string body) : this(
        (int)statusCode, $"[{errorCode}] {body}")
    {
    }

    protected UnsuccessfulAlpacaResponseException(string code, string message) : base(code, message)
    {
    }
}
