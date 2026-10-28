namespace Project.Api.Models.DTOs;

public record DashboardSummaryDto(
    int TotalProjects,
    int TotalTasks,
    TaskStatusCountsDto OverallTaskCounts,
    List<ProjectDto> Projects);
