using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Services;

public interface ITaskService
{
    Task<List<TaskDto>> GetAllForProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<TaskDto> GetByIdAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(Guid userId, Guid projectId, CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(Guid userId, Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskDto> UpdateStatusAsync(Guid userId, Guid projectId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default);
}
