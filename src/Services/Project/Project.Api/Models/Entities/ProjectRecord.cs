namespace Project.Api.Models.Entities;

public record ProjectRecord(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime UpdatedAt);
