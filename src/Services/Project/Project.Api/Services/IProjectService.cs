using Common.Data;
using Project.Api.Models.DTOs;

namespace Project.Api.Services;

public interface IProjectService
{
    Task<FnResult> GetAllAsync(ActorContext actor, string bearerToken, CancellationToken ct);
    Task<FnResult> GetByIdAsync(ActorContext actor, Guid projectId, string bearerToken, CancellationToken ct);
    Task<FnResult> CreateAsync(ActorContext actor, CreateProjectRequest request, CancellationToken ct);
    Task<FnResult> UpdateAsync(ActorContext actor, Guid projectId, UpdateProjectRequest request, CancellationToken ct);
    Task<FnResult> DeleteAsync(ActorContext actor, Guid projectId, CancellationToken ct);
}
