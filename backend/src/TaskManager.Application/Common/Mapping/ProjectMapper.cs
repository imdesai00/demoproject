using TaskManager.Application.DTOs.Projects;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Common.Mapping;

public static class ProjectMapper
{
    public static ProjectDto ToDto(Project project)
    {
        var todo = project.Tasks.Count(t => t.Status == ProjectTaskStatus.Todo);
        var inProgress = project.Tasks.Count(t => t.Status == ProjectTaskStatus.InProgress);
        var done = project.Tasks.Count(t => t.Status == ProjectTaskStatus.Done);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            project.CreatedAt,
            project.UpdatedAt,
            new TaskStatusCounts(todo, inProgress, done));
    }
}
