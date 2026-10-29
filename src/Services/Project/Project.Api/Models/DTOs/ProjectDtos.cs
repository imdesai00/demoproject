namespace Project.Api.Models.DTOs;

public record TaskStatusCountsDto(int Todo, int InProgress, int Done)
{
    public int Total => Todo + InProgress + Done;
}

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    TaskStatusCountsDto TaskCounts);

public record CreateProjectRequest(string Name, string? Description);

public record UpdateProjectRequest(string Name, string? Description);
