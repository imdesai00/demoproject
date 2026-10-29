using Common.Web;
using Identity.Api.Models.DTOs;
using Identity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        (await _authService.RegisterAsync(request, ct)).ToActionResult();

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        (await _authService.LoginAsync(request, ct)).ToActionResult();

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        (await _authService.RefreshAsync(request.RefreshToken, ct)).ToActionResult();

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct) =>
        (await _authService.LogoutAsync(request.RefreshToken, ct)).ToActionResult();
}
