using Common.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Common.Web;

public static class FnResultExtensions
{
    /// <summary>
    /// Maps a Postgres function's {ok, code, message, data} envelope to an HTTP response.
    /// The function's domain "code" is the single source of truth for status — no magic ints,
    /// no leaking exception text.
    /// </summary>
    public static IActionResult ToActionResult(this FnResult result)
    {
        if (result.Ok)
        {
            return new OkObjectResult(result.Data);
        }

        var statusCode = result.Code switch
        {
            "VALIDATION" => StatusCodes.Status400BadRequest,
            "UNAUTHENTICATED" => StatusCodes.Status401Unauthorized,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "INTERNAL" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        // Same {message, errors} shape as ExceptionHandlingMiddleware's ErrorResponse and the
        // ValidationFilter's 400s, so the frontend has one consistent error contract regardless
        // of which layer produced the failure.
        var body = new ErrorResponse
        {
            Message = result.Code == "INTERNAL" ? "An unexpected error occurred. Please try again later." : result.Message ?? result.Code
        };

        return new ObjectResult(body) { StatusCode = statusCode };
    }
}
