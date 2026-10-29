using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Common.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, object payload, CancellationToken ct = default);
}

/// <summary>
/// Publishes domain events to the shared topic exchange. Used directly for simple cases, and by
/// an outbox dispatcher (see each service's OutboxPublisherHostedService) when the event must be
/// published atomically with a database write.
/// </summary>
public sealed class RabbitMqEventPublisher : IEventPublisher
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqSettings _settings;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public RabbitMqEventPublisher(RabbitMqConnectionProvider connectionProvider, RabbitMqSettings settings)
    {
        _connectionProvider = connectionProvider;
        _settings = settings;
    }

    public Task PublishAsync(string routingKey, object payload, CancellationToken ct = default)
    {
        using var channel = _connectionProvider.Connection.CreateModel();
        channel.ExchangeDeclare(_settings.Exchange, ExchangeType.Topic, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions));

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";

        channel.BasicPublish(_settings.Exchange, routingKey, properties, body);
        return Task.CompletedTask;
    }
}
