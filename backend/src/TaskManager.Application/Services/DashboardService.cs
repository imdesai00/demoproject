using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Mapping;
using TaskManager.Application.DTOs.Dashboard;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProjectRepository _projectRepository;

    public DashboardService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var projects = await _projectRepository.GetAllForUserAsync(userId, ct);

        var projectDtos = projects.Select(ProjectMapper.ToDto).ToList();
        var allTasks = projects.SelectMany(p => p.Tasks).ToList();

        var overallCounts = new TaskStatusCounts(
            allTasks.Count(t => t.Status == ProjectTaskStatus.Todo),
            allTasks.Count(t => t.Status == ProjectTaskStatus.InProgress),
            allTasks.Count(t => t.Status == ProjectTaskStatus.Done));

        return new DashboardSummaryDto(
            projectDtos.Count,
            allTasks.Count,
            overallCounts,
            projectDtos);
    }
}
