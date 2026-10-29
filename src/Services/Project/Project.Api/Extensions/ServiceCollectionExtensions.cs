using Common.Data;
using Common.Messaging;
using Common.Security;
using Project.Api.Clients;
using Project.Api.Messaging;
using Project.Api.Repositories;
using Project.Api.Services;

namespace Project.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresFunctions(configuration);
        services.AddJwksValidatedAuth(configuration);
        services.AddEventPublishing(configuration);

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddHttpClient<ITaskServiceClient, TaskServiceClient>(client =>
        {
            var baseUrl = configuration["Services:TaskApiBaseUrl"]
                ?? throw new InvalidOperationException("Services:TaskApiBaseUrl is required.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHostedService<OutboxPublisherHostedService>();

        return services;
    }
}
