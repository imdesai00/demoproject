using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Common.Security;

public interface IAccessTokenIssuer
{
    string IssueAccessToken(Guid userId, string email, string displayName, out DateTime expiresAt);
}

/// <summary>
/// Signs access tokens with this service's RSA private key (RS256) — used only by Identity.Api.
/// Tokens target every downstream service's audience so one token works across the whole app.
/// </summary>
public sealed class AccessTokenIssuer : IAccessTokenIssuer
{
    private readonly ISigningKeyProvider _keyProvider;
    private readonly JwtSettings _settings;

    public AccessTokenIssuer(ISigningKeyProvider keyProvider, JwtSettings settings)
    {
        _keyProvider = keyProvider;
        _settings = settings;
    }

    public string IssueAccessToken(Guid userId, string email, string displayName, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("role", "member"),
            new("email", email),
            new("displayName", displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var audience in _settings.Audiences)
        {
            claims.Add(new Claim("aud", audience));
        }

        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(_keyProvider.GetRsa()) { KeyId = _keyProvider.KeyId },
            SecurityAlgorithms.RsaSha256)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: null, // audiences supplied via explicit "aud" claims above
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
