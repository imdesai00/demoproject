using TaskManager.Domain.Entities;

namespace TaskManager.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task<List<ProjectTask>> GetAllForProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ProjectTask?> GetByIdWithProjectAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ProjectTask task, CancellationToken ct = default);
    void Remove(ProjectTask task);
    Task SaveChangesAsync(CancellationToken ct = default);
}
