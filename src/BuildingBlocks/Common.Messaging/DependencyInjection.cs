using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddEventPublishing(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
            ?? new RabbitMqSettings();

        services.AddSingleton(settings);
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }
}
