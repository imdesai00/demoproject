using System.Security.Claims;
using Common.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Common.Web;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The caller's identity, derived only from the validated JWT (sub + role claims) — never
    /// from the request body or a header.
    /// </summary>
    public static ActorContext Actor(this ClaimsPrincipal user)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Token is missing the 'sub' claim."));
        var role = user.FindFirstValue("role") ?? "member";
        return new ActorContext(userId, role);
    }
}

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActorContext CurrentActor => User.Actor();
}
