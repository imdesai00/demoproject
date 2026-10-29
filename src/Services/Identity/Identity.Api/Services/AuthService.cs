using System.Security.Cryptography;
using System.Text;
using Common.Data;
using Common.Security;
using Identity.Api.Models.DTOs;
using Identity.Api.Repositories;

namespace Identity.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAccessTokenIssuer _tokenIssuer;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAccessTokenIssuer tokenIssuer,
        JwtSettings jwtSettings)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenIssuer = tokenIssuer;
        _jwtSettings = jwtSettings;
    }

    public async Task<FnResult> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var registerResult = await _userRepository.RegisterAsync(
            request.Email.Trim().ToLowerInvariant(), passwordHash, request.DisplayName.Trim(), ct);

        if (!registerResult.Ok || registerResult.Data is null)
        {
            return registerResult;
        }

        var user = UserRepository.MapUser(registerResult.Data.Value);
        return await IssueTokensAsync(new UserDto(user.Id, user.Email, user.DisplayName), ct);
    }

    public async Task<FnResult> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var lookupResult = await _userRepository.FindForLoginAsync(request.Email.Trim().ToLowerInvariant(), ct);

        if (!lookupResult.Ok)
        {
            return lookupResult;
        }

        if (lookupResult.Data is null)
        {
            return new FnResult(false, "UNAUTHENTICATED", "Invalid email or password", null);
        }

        var user = UserRepository.MapUser(lookupResult.Data.Value);
        if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new FnResult(false, "UNAUTHENTICATED", "Invalid email or password", null);
        }

        return await IssueTokensAsync(new UserDto(user.Id, user.Email, user.DisplayName), ct);
    }

    public async Task<FnResult> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var oldHash = HashRefreshToken(refreshToken);
        var newRefreshToken = GenerateRefreshToken();
        var newHash = HashRefreshToken(newRefreshToken);
        var newExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        var rotateResult = await _refreshTokenRepository.RotateAsync(oldHash, newHash, newExpiresAt, ct);
        if (!rotateResult.Ok || rotateResult.Data is null)
        {
            return rotateResult;
        }

        var user = RefreshTokenRepository.MapUser(rotateResult.Data.Value);
        var accessToken = _tokenIssuer.IssueAccessToken(user.Id, user.Email, user.DisplayName, out var accessExpiresAt);

        return Ok(new AuthResponse(
            accessToken, accessExpiresAt, newRefreshToken, newExpiresAt,
            new UserDto(user.Id, user.Email, user.DisplayName)));
    }

    public Task<FnResult> LogoutAsync(string refreshToken, CancellationToken ct) =>
        _refreshTokenRepository.RevokeAsync(HashRefreshToken(refreshToken), ct);

    private async Task<FnResult> IssueTokensAsync(UserDto user, CancellationToken ct)
    {
        var accessToken = _tokenIssuer.IssueAccessToken(user.Id, user.Email, user.DisplayName, out var accessExpiresAt);
        var refreshToken = GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

        var createResult = await _refreshTokenRepository.CreateAsync(user.Id, HashRefreshToken(refreshToken), refreshExpiresAt, ct);
        if (!createResult.Ok)
        {
            return createResult;
        }

        return Ok(new AuthResponse(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt, user));
    }

    private static FnResult Ok(AuthResponse response) =>
        new(true, "OK", null, System.Text.Json.JsonSerializer.SerializeToElement(response, SerializerOptions));

    private static readonly System.Text.Json.JsonSerializerOptions SerializerOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
