namespace Task.Api.Models.DTOs;

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateOnly? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateTaskRequest(Guid ProjectId, string Title, string? Description, string Priority, DateOnly? DueDate);

public record UpdateTaskRequest(string Title, string? Description, string Priority, DateOnly? DueDate);

public record UpdateTaskStatusRequest(string Status);

public record StatusCountsEntry(Guid ProjectId, int Todo, int InProgress, int Done);

public record StatusCountsRequest(List<Guid> ProjectIds);
