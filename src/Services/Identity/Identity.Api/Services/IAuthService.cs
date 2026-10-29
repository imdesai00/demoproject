using Common.Data;
using Identity.Api.Models.DTOs;

namespace Identity.Api.Services;

public interface IAuthService
{
    Task<FnResult> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<FnResult> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<FnResult> RefreshAsync(string refreshToken, CancellationToken ct);
    Task<FnResult> LogoutAsync(string refreshToken, CancellationToken ct);
}
