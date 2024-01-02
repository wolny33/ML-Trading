using System.Net;
using System.Net.Sockets;
using Alpaca.Markets;
using TradingBot.Exceptions;
using TradingBot.Exceptions.Alpaca;
using ILogger = Serilog.ILogger;

namespace TradingBot;

public static class AlpacaClientExtensions
{
    public static async Task<T> ExecuteWithErrorHandling<T>(this Task<T> alpacaCall, ILogger? logger = null)
    {
        try
        {
            return await alpacaCall;
        }
        catch (RequestValidationException e)
        {
            logger?.Error(e, "Alpaca request failed validation");
            throw new BadAlpacaRequestException(e);
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode is { } statusCode)
        {
            logger?.Error(e, "Alpaca responded with {StatusCode}", statusCode);
            throw new UnsuccessfulAlpacaResponseException(statusCode, e.ErrorCode, e.Message);
        }
        catch (Exception e) when (e is RestClientErrorException or HttpRequestException or SocketException)
        {
            logger?.Error(e, "Alpaca request failed");
            throw new AlpacaCallFailedException(e);
        }
    }

    public static async Task<T?> ReturnNullOnRequestLimit<T>(this Task<T> alpacaCall, ILogger? logger = null)
        where T : class
    {
        try
        {
            return await alpacaCall;
        }
        catch (RestClientErrorException e) when (e.HttpStatusCode == HttpStatusCode.TooManyRequests)
        {
            logger?.Debug("Alpaca call failed because request limit was reached");
            return null;
        }
    }
}
