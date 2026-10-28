using Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[AllowAnonymous]
public class JwksController : ControllerBase
{
    private readonly ISigningKeyProvider _keyProvider;

    public JwksController(ISigningKeyProvider keyProvider)
    {
        _keyProvider = keyProvider;
    }

    /// <summary>Public key set Project.Api/Task.Api fetch to validate RS256 access tokens.</summary>
    [HttpGet("/.well-known/jwks.json")]
    public IActionResult GetJwks() => Ok(JwksBuilder.BuildPublicJwks(_keyProvider));
}
