using Common.Data;

namespace Project.Api.Repositories;

public record OutboxEvent(Guid Id, string EventType, string RoutingKey, System.Text.Json.JsonElement Payload);

public interface IOutboxRepository
{
    Task<FnResult> DequeueAsync(int batchSize, CancellationToken ct);
    Task<FnResult> MarkPublishedAsync(IEnumerable<Guid> ids, CancellationToken ct);
}

/// <summary>
/// Used only by OutboxPublisherHostedService — always calls with ActorContext.System, which the
/// underlying functions require (they refuse any other role). Never exposed via HTTP.
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly IPgFunctionExecutor _db;

    public OutboxRepository(IPgFunctionExecutor db)
    {
        _db = db;
    }

    public Task<FnResult> DequeueAsync(int batchSize, CancellationToken ct) =>
        _db.CallAsync("project.fn_dequeue_outbox_events", ActorContext.System, new { batch_size = batchSize }, ct);

    public Task<FnResult> MarkPublishedAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        _db.CallAsync("project.fn_mark_outbox_published", ActorContext.System, new { ids }, ct);

    public static List<OutboxEvent> MapEvents(System.Text.Json.JsonElement data) =>
        [.. data.EnumerateArray().Select(e => new OutboxEvent(
            e.GetGuid("id"), e.GetString("event_type"), e.GetString("routing_key"), e.GetProperty("payload")))];
}
