namespace TradingBot.Exceptions;

public class CallFailedException : ApiCallException
{
    protected CallFailedException(string apiName, Exception reason) : base("api-call-failed",
        $"Call to {apiName} failed: {reason.Message}", reason)
    {
        StatusCode = StatusCodes.Status503ServiceUnavailable;
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
