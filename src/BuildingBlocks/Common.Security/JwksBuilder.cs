using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Common.Security;

public static class JwksBuilder
{
    /// <summary>
    /// Builds the public-key-only JWKS document this service's /.well-known/jwks.json endpoint
    /// serves. Explicitly strips to public parameters first — RsaSecurityKey wrapping the full
    /// RSA object (as returned by ISigningKeyProvider) would otherwise serialize the PRIVATE
    /// key components (d, p, q, dp, dq, qi) into the JWK, which must never leave this service.
    /// </summary>
    public static JsonWebKeySet BuildPublicJwks(ISigningKeyProvider keyProvider)
    {
        var publicParameters = keyProvider.GetRsa().ExportParameters(includePrivateParameters: false);
        using var publicOnlyRsa = RSA.Create(publicParameters);

        var rsaSecurityKey = new RsaSecurityKey(publicOnlyRsa) { KeyId = keyProvider.KeyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaSecurityKey);
        jwk.Use = "sig";
        jwk.Alg = SecurityAlgorithms.RsaSha256;

        var jwks = new JsonWebKeySet();
        jwks.Keys.Add(jwk);
        return jwks;
    }
}

