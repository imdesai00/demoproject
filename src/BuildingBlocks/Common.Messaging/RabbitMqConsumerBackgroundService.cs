using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Common.Messaging;

/// <summary>
/// Background consumer for one durable queue bound to one routing key on the shared topic
/// exchange. A failed handler nacks the message with requeue — RabbitMQ redelivers it, so a
/// transient DB blip doesn't silently lose the event. Subclass and implement HandleAsync.
/// </summary>
public abstract class RabbitMqConsumerBackgroundService : BackgroundService
{
    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger _logger;
    private readonly string _queueName;
    private readonly string _routingKey;

    protected RabbitMqConsumerBackgroundService(
        RabbitMqConnectionProvider connectionProvider,
        RabbitMqSettings settings,
        ILogger logger,
        string queueName,
        string routingKey)
    {
        _connectionProvider = connectionProvider;
        _settings = settings;
        _logger = logger;
        _queueName = queueName;
        _routingKey = routingKey;
    }

    protected abstract Task HandleAsync(string routingKey, string jsonBody, CancellationToken ct);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = _connectionProvider.Connection.CreateModel();
        channel.ExchangeDeclare(_settings.Exchange, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_queueName, _settings.Exchange, _routingKey);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await HandleAsync(ea.RoutingKey, body, stoppingToken);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message with routing key {RoutingKey}; requeuing", ea.RoutingKey);
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(_queueName, autoAck: false, consumer);

        stoppingToken.Register(() => channel.Dispose());
        return Task.CompletedTask;
    }
}
