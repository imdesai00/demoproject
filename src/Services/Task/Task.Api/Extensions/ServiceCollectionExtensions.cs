using Common.Data;
using Common.Messaging;
using Common.Security;
using Task.Api.Clients;
using Task.Api.Messaging;
using Task.Api.Repositories;
using Task.Api.Services;

namespace Task.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPostgresFunctions(configuration);
        services.AddJwksValidatedAuth(configuration);
        services.AddEventPublishing(configuration);

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ISystemTaskRepository, SystemTaskRepository>();
        services.AddScoped<ITaskService, TaskService>();

        services.AddHttpClient<IProjectServiceClient, ProjectServiceClient>(client =>
        {
            var baseUrl = configuration["Services:ProjectApiBaseUrl"]
                ?? throw new InvalidOperationException("Services:ProjectApiBaseUrl is required.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHostedService<ProjectDeletedConsumer>();

        return services;
    }
}
