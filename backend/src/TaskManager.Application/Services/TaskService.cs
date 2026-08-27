using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;

    public TaskService(ITaskRepository taskRepository, IProjectRepository projectRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
    }

    public async Task<List<TaskDto>> GetAllForProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default)
    {
        await GetOwnedProjectAsync(userId, projectId, ct);
        var tasks = await _taskRepository.GetAllForProjectAsync(projectId, ct);
        return tasks.Select(ToDto).ToList();
    }

    public async Task<TaskDto> GetByIdAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskAsync(userId, projectId, taskId, ct);
        return ToDto(task);
    }

    public async Task<TaskDto> CreateAsync(Guid userId, Guid projectId, CreateTaskRequest request, CancellationToken ct = default)
    {
        await GetOwnedProjectAsync(userId, projectId, ct);

        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task, ct);
        await _taskRepository.SaveChangesAsync(ct);

        return ToDto(task);
    }

    public async Task<TaskDto> UpdateAsync(Guid userId, Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskAsync(userId, projectId, taskId, ct);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync(ct);

        return ToDto(task);
    }

    public async Task<TaskDto> UpdateStatusAsync(Guid userId, Guid projectId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskAsync(userId, projectId, taskId, ct);

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync(ct);

        return ToDto(task);
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct = default)
    {
        var task = await GetOwnedTaskAsync(userId, projectId, taskId, ct);
        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync(ct);
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

    private async Task<ProjectTask> GetOwnedTaskAsync(Guid userId, Guid projectId, Guid taskId, CancellationToken ct)
    {
        var task = await _taskRepository.GetByIdWithProjectAsync(taskId, ct)
            ?? throw new NotFoundException(nameof(ProjectTask), taskId);

        if (task.ProjectId != projectId || task.Project is null || task.Project.UserId != userId)
        {
            throw new ForbiddenAccessException();
        }

        return task;
    }

    private static TaskDto ToDto(ProjectTask task) => new(
        task.Id,
        task.ProjectId,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.DueDate,
        task.CreatedAt,
        task.UpdatedAt);
}
