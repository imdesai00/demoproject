using Common.Messaging;
using Project.Api.Repositories;

namespace Project.Api.Messaging;

/// <summary>
/// Polls project.outbox_events for rows fn_delete_project wrote in the same transaction as the
/// delete, publishes each to RabbitMQ, then marks it published. This is what makes the outbox
/// pattern work: the DB write and "an event exists" can never disagree, and a publish failure
/// just means the row stays unpublished and gets retried next poll.
/// </summary>
public class OutboxPublisherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherHostedService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    public OutboxPublisherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publish cycle failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }

    private async Task PublishPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var dequeueResult = await outboxRepository.DequeueAsync(batchSize: 20, ct);
        if (!dequeueResult.Ok || dequeueResult.Data is null)
        {
            return;
        }

        var events = OutboxRepository.MapEvents(dequeueResult.Data.Value);
        if (events.Count == 0)
        {
            return;
        }

        var publishedIds = new List<Guid>();
        foreach (var @event in events)
        {
            try
            {
                await eventPublisher.PublishAsync(@event.RoutingKey, @event.Payload, ct);
                publishedIds.Add(@event.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId} ({EventType}); will retry next poll",
                    @event.Id, @event.EventType);
            }
        }

        if (publishedIds.Count > 0)
        {
            await outboxRepository.MarkPublishedAsync(publishedIds, ct);
            _logger.LogInformation("Published {Count} outbox event(s)", publishedIds.Count);
        }
    }
}
