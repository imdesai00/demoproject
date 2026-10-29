using System.Text.Json.Serialization;
using Common.Messaging;
using Task.Api.Repositories;

namespace Task.Api.Messaging;

internal record ProjectDeletedEvent(
    [property: JsonPropertyName("project_id")] Guid ProjectId,
    [property: JsonPropertyName("user_id")] Guid UserId);

/// <summary>
/// Consumes "project.deleted" (published by Project.Api's outbox) and deletes every task under
/// that project. This is what keeps the two services' data consistent now that there's no
/// database-level cascade delete across service boundaries.
/// </summary>
public class ProjectDeletedConsumer : RabbitMqConsumerBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProjectDeletedConsumer> _logger;

    public ProjectDeletedConsumer(
        RabbitMqConnectionProvider connectionProvider,
        RabbitMqSettings settings,
        ILogger<ProjectDeletedConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(connectionProvider, settings, logger, queueName: "task-api.project-deleted", routingKey: "project.deleted")
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async System.Threading.Tasks.Task HandleAsync(string routingKey, string jsonBody, CancellationToken ct)
    {
        var @event = System.Text.Json.JsonSerializer.Deserialize<ProjectDeletedEvent>(
            jsonBody, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));

        if (@event is null || @event.ProjectId == Guid.Empty)
        {
            _logger.LogWarning("Received malformed project.deleted event, skipping: {Body}", jsonBody);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISystemTaskRepository>();
        var result = await repository.DeleteAllForProjectAsync(@event.ProjectId, ct);

        if (!result.Ok)
        {
            throw new InvalidOperationException($"fn_delete_tasks_for_project failed: {result.Code} {result.Message}");
        }

        _logger.LogInformation("Deleted tasks for deleted project {ProjectId}", @event.ProjectId);
    }
}
