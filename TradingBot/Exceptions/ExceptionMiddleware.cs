using Newtonsoft.Json;

namespace TradingBot.Exceptions;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ResponseException e)
        {
            await HandleResponseExceptionAsync(e, context);
        }
    }

    private static Task HandleResponseExceptionAsync(ResponseException exception, HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;

        return context.Response.WriteAsync(JsonConvert.SerializeObject(exception.ToResponse(), Formatting.Indented));
    }
}
