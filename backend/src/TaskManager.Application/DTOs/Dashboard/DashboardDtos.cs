using TaskManager.Application.DTOs.Projects;

namespace TaskManager.Application.DTOs.Dashboard;

public record DashboardSummaryDto(
    int TotalProjects,
    int TotalTasks,
    TaskStatusCounts OverallTaskCounts,
    List<ProjectDto> Projects);
