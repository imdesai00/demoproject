using Microsoft.AspNetCore.Http;

namespace Common.Web;

public static class HttpContextExtensions
{
    /// <summary>The raw bearer token this request was authenticated with, for forwarding to another service.</summary>
    public static string GetBearerToken(this HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..] : string.Empty;
    }
}
