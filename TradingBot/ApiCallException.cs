namespace TradingBot;

public abstract class ApiCallException : Exception
{
    protected ApiCallException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public class UnsuccessfulResponseException : ApiCallException
{
    protected UnsuccessfulResponseException(string apiName, int statusCode, string body) : base(
        $"{apiName} responded with status code {statusCode}: {body}")
    {
    }
}

public sealed class UnsuccessfulPredictorResponseException : UnsuccessfulResponseException
{
    public UnsuccessfulPredictorResponseException(int statusCode, string body) : base("Predictor service", statusCode,
        body)
    {
        Data["StatusCode"] = statusCode;
        Data["Response"] = body;
    }
}

public sealed class UnsuccessfulAlpacaResponseException : UnsuccessfulResponseException
{
    public UnsuccessfulAlpacaResponseException(int statusCode, string body) : base("Alpaca API", statusCode, body)
    {
        Data["StatusCode"] = statusCode;
        Data["Response"] = body;
    }
}

public class CallFailedException : ApiCallException
{
    protected CallFailedException(string apiName, Exception reason) : base($"Call to {apiName} failed", reason)
    {
    }
}

public sealed class PredictorCallFailedException : CallFailedException
{
    public PredictorCallFailedException(Exception reason) : base("predictor service", reason)
    {
    }
}

public sealed class AlpacaCallFailedException : CallFailedException
{
    public AlpacaCallFailedException(Exception reason) : base("Alpaca API", reason)
    {
    }
}
