using Common.Data;
using Task.Api.Models.DTOs;

namespace Task.Api.Services;

public interface ITaskService
{
    System.Threading.Tasks.Task<FnResult> GetAllForProjectAsync(ActorContext actor, Guid projectId, CancellationToken ct);
    System.Threading.Tasks.Task<FnResult> GetByIdAsync(ActorContext actor, Guid taskId, CancellationToken ct);
    System.Threading.Tasks.Task<FnResult> CreateAsync(ActorContext actor, CreateTaskRequest request, string bearerToken, CancellationToken ct);
    System.Threading.Tasks.Task<FnResult> UpdateAsync(ActorContext actor, Guid taskId, UpdateTaskRequest request, CancellationToken ct);
    System.Threading.Tasks.Task<FnResult> UpdateStatusAsync(ActorContext actor, Guid taskId, UpdateTaskStatusRequest request, CancellationToken ct);
    System.Threading.Tasks.Task<FnResult> DeleteAsync(ActorContext actor, Guid taskId, CancellationToken ct);
    System.Threading.Tasks.Task<FnResult> GetStatusCountsAsync(ActorContext actor, List<Guid> projectIds, CancellationToken ct);
}
