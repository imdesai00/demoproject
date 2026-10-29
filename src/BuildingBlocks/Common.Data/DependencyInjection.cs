using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Common.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Registers one pooled NpgsqlDataSource for this service's own database (never another
    /// service's) plus the single door to it, <see cref="IPgFunctionExecutor"/>.
    /// </summary>
    public static IServiceCollection AddPostgresFunctions(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Db")
            ?? throw new InvalidOperationException("Connection string 'Db' is not configured.");

        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
        services.AddSingleton<IPgFunctionExecutor, PgFunctionExecutor>();

        return services;
    }
}
