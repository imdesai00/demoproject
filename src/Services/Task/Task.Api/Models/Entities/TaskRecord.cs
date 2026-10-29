namespace Task.Api.Models.Entities;

public record TaskRecord(
    Guid Id, Guid ProjectId, string Title, string? Description,
    string Status, string Priority, DateOnly? DueDate, DateTime CreatedAt, DateTime UpdatedAt);
