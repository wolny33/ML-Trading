namespace TradingBot;

public class ApiCallException : Exception
{
    public ApiCallException(string apiName, int statusCode, string body) : base(
        $"{apiName} responded with status code {statusCode}: {body}")
    {
    }
}

public sealed class PredictorCallException : ApiCallException
{
    public PredictorCallException(int statusCode, string body) : base("Predictor service", statusCode, body)
    {
        Data["StatusCode"] = statusCode;
        Data["Response"] = body;
    }
}
