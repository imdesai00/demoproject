using System.Net;
using System.Text.Json;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Api.Middleware;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
}

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse();
        HttpStatusCode statusCode;

        switch (exception)
        {
            case ValidationAppException validationException:
                statusCode = HttpStatusCode.BadRequest;
                response.Message = validationException.Message;
                response.Errors = validationException.Errors;
                break;
            case NotFoundException notFoundException:
                statusCode = HttpStatusCode.NotFound;
                response.Message = notFoundException.Message;
                break;
            case ForbiddenAccessException forbiddenException:
                statusCode = HttpStatusCode.Forbidden;
                response.Message = forbiddenException.Message;
                break;
            case Application.Common.Exceptions.AuthenticationException authException:
                statusCode = HttpStatusCode.Unauthorized;
                response.Message = authException.Message;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred. Please try again later.";
                _logger.LogError(exception, "Unhandled exception occurred");
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
