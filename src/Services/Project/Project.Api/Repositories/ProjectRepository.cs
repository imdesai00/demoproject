using Common.Data;
using Project.Api.Models.Entities;

namespace Project.Api.Repositories;

public interface IProjectRepository
{
    Task<FnResult> CreateAsync(ActorContext actor, string name, string? description, CancellationToken ct);
    Task<FnResult> GetAllAsync(ActorContext actor, CancellationToken ct);
    Task<FnResult> GetByIdAsync(ActorContext actor, Guid projectId, CancellationToken ct);
    Task<FnResult> UpdateAsync(ActorContext actor, Guid projectId, string name, string? description, CancellationToken ct);
    Task<FnResult> DeleteAsync(ActorContext actor, Guid projectId, CancellationToken ct);
}

public class ProjectRepository : IProjectRepository
{
    private readonly IPgFunctionExecutor _db;

    public ProjectRepository(IPgFunctionExecutor db)
    {
        _db = db;
    }

    public Task<FnResult> CreateAsync(ActorContext actor, string name, string? description, CancellationToken ct) =>
        _db.CallAsync("project.fn_create_project", actor, new { name, description }, ct);

    public Task<FnResult> GetAllAsync(ActorContext actor, CancellationToken ct) =>
        _db.CallAsync("project.fn_get_projects", actor, null, ct);

    public Task<FnResult> GetByIdAsync(ActorContext actor, Guid projectId, CancellationToken ct) =>
        _db.CallAsync("project.fn_get_project", actor, new { project_id = projectId }, ct);

    public Task<FnResult> UpdateAsync(ActorContext actor, Guid projectId, string name, string? description, CancellationToken ct) =>
        _db.CallAsync("project.fn_update_project", actor, new { project_id = projectId, name, description }, ct);

    public Task<FnResult> DeleteAsync(ActorContext actor, Guid projectId, CancellationToken ct) =>
        _db.CallAsync("project.fn_delete_project", actor, new { project_id = projectId }, ct);

    public static ProjectRecord MapProject(System.Text.Json.JsonElement data) => new(
        data.GetGuid("id"),
        data.GetString("name"),
        data.GetNullableString("description"),
        data.GetDateTime("created_at"),
        data.GetDateTime("updated_at"));
}
