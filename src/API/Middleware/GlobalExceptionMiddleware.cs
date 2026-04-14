using System.Net;
using System.Text.Json;

namespace IntelliImport.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = JsonSerializer.Serialize(new
            {
                error   = "An unexpected error occurred.",
                detail  = ex.Message,
                traceId = context.TraceIdentifier
            });
            await context.Response.WriteAsync(response);
        }
    }
}
