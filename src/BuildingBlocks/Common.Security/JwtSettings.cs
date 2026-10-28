namespace Common.Security;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>The identity service's issuer name, e.g. "identity-service".</summary>
    public string Issuer { get; set; } = "identity-service";

    /// <summary>This service's own audience value, e.g. "project-service". Used by validating services.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>All audiences a newly issued access token should target. Used only by Identity.Api.</summary>
    public string[] Audiences { get; set; } = [];

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>Where Identity.Api persists its RSA signing key (dev/demo; a real deployment uses a KMS/secret store).</summary>
    public string SigningKeyPath { get; set; } = "keys/identity-signing-key.pem";

    /// <summary>Where Project.Api/Task.Api fetch Identity.Api's public key from.</summary>
    public string JwksUrl { get; set; } = string.Empty;
}
