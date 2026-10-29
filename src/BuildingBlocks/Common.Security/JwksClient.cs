using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Common.Security;

/// <summary>
/// Fetches and caches Identity.Api's public JWKS so Project.Api/Task.Api can validate RS256
/// tokens without ever holding (or being configured with) a shared secret. Used as the
/// IssuerSigningKeyResolver for JWT bearer validation.
/// </summary>
public sealed class JwksClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JwksClient> _logger;
    private readonly string _jwksUrl;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyDictionary<string, SecurityKey> _cache = new ConcurrentDictionary<string, SecurityKey>();
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public JwksClient(HttpClient httpClient, ILogger<JwksClient> logger, string jwksUrl)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jwksUrl = jwksUrl;
    }

    /// <summary>
    /// Matches the IssuerSigningKeyResolver delegate signature used by TokenValidationParameters.
    /// Called synchronously by the JWT bearer handler, so we block on the (cached, fast) fetch.
    /// </summary>
    public IEnumerable<SecurityKey> ResolveSigningKey(
        string token, Microsoft.IdentityModel.Tokens.SecurityToken securityToken,
        string? kid, TokenValidationParameters validationParameters)
    {
        var keys = GetKeysAsync(forceRefresh: false).GetAwaiter().GetResult();

        if (kid is not null && keys.TryGetValue(kid, out var match))
        {
            return [match];
        }

        // kid missing from cache (e.g. key rotated) — refresh once and retry before giving up.
        keys = GetKeysAsync(forceRefresh: true).GetAwaiter().GetResult();
        return kid is not null && keys.TryGetValue(kid, out var refreshed) ? [refreshed] : keys.Values;
    }

    private async Task<IReadOnlyDictionary<string, SecurityKey>> GetKeysAsync(bool forceRefresh)
    {
        if (!forceRefresh && DateTimeOffset.UtcNow - _cachedAt < CacheTtl && _cache.Count > 0)
        {
            return _cache;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (!forceRefresh && DateTimeOffset.UtcNow - _cachedAt < CacheTtl && _cache.Count > 0)
            {
                return _cache;
            }

            var jwks = await _httpClient.GetFromJsonAsync<JsonWebKeySet>(_jwksUrl);
            if (jwks is null)
            {
                _logger.LogWarning("JWKS fetch from {Url} returned no content", _jwksUrl);
                return _cache;
            }

            var map = new ConcurrentDictionary<string, SecurityKey>();
            foreach (var key in jwks.Keys)
            {
                map[key.Kid] = key;
            }

            _cache = map;
            _cachedAt = DateTimeOffset.UtcNow;
            return _cache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch JWKS from {Url}", _jwksUrl);
            return _cache;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
