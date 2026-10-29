using Common.Data;
using Task.Api.Models.Entities;

namespace Task.Api.Repositories;

public interface ITaskRepository
{
    Task<FnResult> CreateAsync(ActorContext actor, Guid projectId, string title, string? description, string priority, DateOnly? dueDate, CancellationToken ct);
    Task<FnResult> GetAllForProjectAsync(ActorContext actor, Guid projectId, CancellationToken ct);
    Task<FnResult> GetByIdAsync(ActorContext actor, Guid taskId, CancellationToken ct);
    Task<FnResult> UpdateAsync(ActorContext actor, Guid taskId, string title, string? description, string priority, DateOnly? dueDate, CancellationToken ct);
    Task<FnResult> UpdateStatusAsync(ActorContext actor, Guid taskId, string status, CancellationToken ct);
    Task<FnResult> DeleteAsync(ActorContext actor, Guid taskId, CancellationToken ct);
    Task<FnResult> GetStatusCountsAsync(ActorContext actor, IEnumerable<Guid> projectIds, CancellationToken ct);
}

public class TaskRepository : ITaskRepository
{
    private readonly IPgFunctionExecutor _db;

    public TaskRepository(IPgFunctionExecutor db)
    {
        _db = db;
    }

    public Task<FnResult> CreateAsync(ActorContext actor, Guid projectId, string title, string? description, string priority, DateOnly? dueDate, CancellationToken ct) =>
        _db.CallAsync("task.fn_create_task", actor, new { project_id = projectId, title, description, priority, due_date = dueDate }, ct);

    public Task<FnResult> GetAllForProjectAsync(ActorContext actor, Guid projectId, CancellationToken ct) =>
        _db.CallAsync("task.fn_get_tasks_for_project", actor, new { project_id = projectId }, ct);

    public Task<FnResult> GetByIdAsync(ActorContext actor, Guid taskId, CancellationToken ct) =>
        _db.CallAsync("task.fn_get_task", actor, new { task_id = taskId }, ct);

    public Task<FnResult> UpdateAsync(ActorContext actor, Guid taskId, string title, string? description, string priority, DateOnly? dueDate, CancellationToken ct) =>
        _db.CallAsync("task.fn_update_task", actor, new { task_id = taskId, title, description, priority, due_date = dueDate }, ct);

    public Task<FnResult> UpdateStatusAsync(ActorContext actor, Guid taskId, string status, CancellationToken ct) =>
        _db.CallAsync("task.fn_update_task_status", actor, new { task_id = taskId, status }, ct);

    public Task<FnResult> DeleteAsync(ActorContext actor, Guid taskId, CancellationToken ct) =>
        _db.CallAsync("task.fn_delete_task", actor, new { task_id = taskId }, ct);

    public Task<FnResult> GetStatusCountsAsync(ActorContext actor, IEnumerable<Guid> projectIds, CancellationToken ct) =>
        _db.CallAsync("task.fn_get_status_counts", actor, new { project_ids = projectIds }, ct);

    public static TaskRecord MapTask(System.Text.Json.JsonElement data) => new(
        data.GetGuid("id"), data.GetGuid("project_id"), data.GetString("title"), data.GetNullableString("description"),
        data.GetString("status"), data.GetString("priority"), data.GetNullableDateOnly("due_date"),
        data.GetDateTime("created_at"), data.GetDateTime("updated_at"));
}
