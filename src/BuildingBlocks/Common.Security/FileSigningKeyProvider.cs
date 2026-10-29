using System.Security.Cryptography;

namespace Common.Security;

/// <summary>
/// Owns this service's RSA signing key pair. Only Identity.Api uses this (to sign tokens and
/// publish its public key via JWKS) — Project.Api/Task.Api never hold a private key, they only
/// fetch the public key through <see cref="JwksClient"/>.
/// </summary>
public interface ISigningKeyProvider
{
    RSA GetRsa();
    string KeyId { get; }
}

/// <summary>
/// Loads an RSA key pair from a PEM file if present, otherwise generates one and persists it —
/// so restarting the service doesn't invalidate every outstanding token. The key id (kid) is a
/// stable hash of the public key modulus, so it doesn't change across restarts either.
/// </summary>
public sealed class FileSigningKeyProvider : ISigningKeyProvider
{
    private readonly RSA _rsa;

    public FileSigningKeyProvider(string keyFilePath)
    {
        var directory = Path.GetDirectoryName(keyFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _rsa = RSA.Create(2048);

        if (File.Exists(keyFilePath))
        {
            _rsa.ImportFromPem(File.ReadAllText(keyFilePath));
        }
        else
        {
            File.WriteAllText(keyFilePath, _rsa.ExportRSAPrivateKeyPem());
        }

        var modulus = _rsa.ExportParameters(false).Modulus!;
        KeyId = Convert.ToHexString(SHA256.HashData(modulus))[..16].ToLowerInvariant();
    }

    public RSA GetRsa() => _rsa;

    public string KeyId { get; }
}
