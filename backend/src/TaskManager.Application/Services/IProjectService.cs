using TaskManager.Application.DTOs.Projects;

namespace TaskManager.Application.Services;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllForUserAsync(Guid userId, CancellationToken ct = default);
    Task<ProjectDto> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<ProjectDto> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default);
    Task<ProjectDto> UpdateAsync(Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, Guid projectId, CancellationToken ct = default);
}
