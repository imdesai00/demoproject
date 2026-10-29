using Common.Data;
using Common.Security;
using Common.Web;
using Identity.Api.Repositories;
using Identity.Api.Services;

namespace Identity.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresFunctions(configuration);

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        services.AddSingleton(jwtSettings);

        services.AddSingleton<ISigningKeyProvider>(_ => new FileSigningKeyProvider(jwtSettings.SigningKeyPath));
        services.AddSingleton<IAccessTokenIssuer, AccessTokenIssuer>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
