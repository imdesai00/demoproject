using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Common.Security;

public static class DependencyInjection
{
    /// <summary>
    /// Wires RS256 JWT bearer validation for a downstream service: no local secret, no shared
    /// HS256 key — the signing key is fetched from Identity.Api's JWKS endpoint and cached.
    /// Also sets a deny-by-default fallback authorization policy (every endpoint requires an
    /// authenticated user unless explicitly marked [AllowAnonymous]).
    /// </summary>
    public static IServiceCollection AddJwksValidatedAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        if (string.IsNullOrWhiteSpace(settings.JwksUrl))
        {
            throw new InvalidOperationException("Jwt:JwksUrl is required.");
        }

        services.AddSingleton(settings);
        services.AddHttpClient<JwksClient>(client => client.Timeout = TimeSpan.FromSeconds(5));
        services.AddSingleton(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(JwksClient));
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JwksClient>>();
            return new JwksClient(httpClient, logger, settings.JwksUrl);
        });

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false; // dev/compose network; terminate TLS at the gateway in real deployments
            });

        // Bound via DI (not the AddJwtBearer configure lambda above, which can't take injected
        // services) so JwksClient.ResolveSigningKey is wired without building a second container.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwksClient>((options, jwksClient) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeyResolver = jwksClient.ResolveSigningKey,
                    ValidateLifetime = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
