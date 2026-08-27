using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Mapping;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<List<ProjectDto>> GetAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var projects = await _projectRepository.GetAllForUserAsync(userId, ct);
        return projects.Select(ToDto).ToList();
    }

    public async Task<ProjectDto> GetByIdAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var project = await GetOwnedProjectWithTasksAsync(userId, projectId, ct);
        return ToDto(project);
    }

    public async Task<ProjectDto> CreateAsync(Guid userId, CreateProjectRequest request, CancellationToken ct = default)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projectRepository.AddAsync(project, ct);
        await _projectRepository.SaveChangesAsync(ct);

        return ToDto(project);
    }

    public async Task<ProjectDto> UpdateAsync(Guid userId, Guid projectId, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await GetOwnedProjectWithTasksAsync(userId, projectId, ct);

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync(ct);

        return ToDto(project);
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var project = await GetOwnedProjectAsync(userId, projectId, ct);
        _projectRepository.Remove(project);
        await _projectRepository.SaveChangesAsync(ct);
    }

    private async Task<Project> GetOwnedProjectAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, ct)
            ?? throw new NotFoundException(nameof(Project), projectId);

        if (project.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        return project;
    }

    private async Task<Project> GetOwnedProjectWithTasksAsync(Guid userId, Guid projectId, CancellationToken ct)
    {
        var project = await _projectRepository.GetByIdWithTasksAsync(projectId, ct)
            ?? throw new NotFoundException(nameof(Project), projectId);

        if (project.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        return project;
    }

    private static ProjectDto ToDto(Project project) => ProjectMapper.ToDto(project);
}
