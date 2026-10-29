using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Common.Web;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Last-resort safety net. In this architecture almost every domain error is returned as a
/// normal FnResult (via ToActionResult) and never reaches here — this only catches genuinely
/// unexpected exceptions (a bug, a network blip talking to another service) and makes sure the
/// client never sees an exception message or stack trace.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            await context.Response.WriteAsync(JsonSerializer.Serialize(
                new ErrorResponse { Message = "An unexpected error occurred. Please try again later." },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
