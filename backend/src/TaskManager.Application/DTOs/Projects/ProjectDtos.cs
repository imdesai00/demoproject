namespace TaskManager.Application.DTOs.Projects;

public record TaskStatusCounts(int Todo, int InProgress, int Done)
{
    public int Total => Todo + InProgress + Done;
}

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    TaskStatusCounts TaskCounts);

public record CreateProjectRequest(string Name, string? Description);

public record UpdateProjectRequest(string Name, string? Description);
