using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Tasks;

public record TaskDto(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate);

public record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate);

public record UpdateTaskStatusRequest(ProjectTaskStatus Status);
