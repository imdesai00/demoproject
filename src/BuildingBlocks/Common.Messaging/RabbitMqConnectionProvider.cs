using RabbitMQ.Client;

namespace Common.Messaging;

/// <summary>
/// Owns a single long-lived RabbitMQ connection for this service (both publishing and
/// consuming share it — RabbitMQ connections are cheap to hold, channels are what you open
/// per operation).
/// </summary>
public sealed class RabbitMqConnectionProvider : IDisposable
{
    private readonly Lazy<IConnection> _connection;

    public RabbitMqConnectionProvider(RabbitMqSettings settings)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };

        _connection = new Lazy<IConnection>(() => factory.CreateConnection());
    }

    public IConnection Connection => _connection.Value;

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
