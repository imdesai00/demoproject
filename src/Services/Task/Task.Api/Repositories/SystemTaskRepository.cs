using Common.Data;

namespace Task.Api.Repositories;

/// <summary>Used only by ProjectDeletedConsumer (ActorContext.System) — never exposed via HTTP.</summary>
public interface ISystemTaskRepository
{
    Task<FnResult> DeleteAllForProjectAsync(Guid projectId, CancellationToken ct);
}

public class SystemTaskRepository : ISystemTaskRepository
{
    private readonly IPgFunctionExecutor _db;

    public SystemTaskRepository(IPgFunctionExecutor db)
    {
        _db = db;
    }

    public Task<FnResult> DeleteAllForProjectAsync(Guid projectId, CancellationToken ct) =>
        _db.CallAsync("task.fn_delete_tasks_for_project", ActorContext.System, new { project_id = projectId }, ct);
}
