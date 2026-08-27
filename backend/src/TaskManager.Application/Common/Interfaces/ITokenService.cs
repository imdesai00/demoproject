using TaskManager.Domain.Entities;

namespace TaskManager.Application.Common.Interfaces;

public record GeneratedTokens(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

public interface ITokenService
{
    string GenerateAccessToken(User user, out DateTime expiresAt);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
