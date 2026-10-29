using System.Text.Json;
using Common.Data;
using Project.Api.Models.DTOs;

namespace Project.Api.Services;

public class DashboardService : IDashboardService
{
    private readonly IProjectService _projectService;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public DashboardService(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public async Task<FnResult> GetSummaryAsync(ActorContext actor, string bearerToken, CancellationToken ct)
    {
        var projectsResult = await _projectService.GetAllAsync(actor, bearerToken, ct);
        if (!projectsResult.Ok || projectsResult.Data is null)
        {
            return projectsResult;
        }

        var projects = JsonSerializer.Deserialize<List<ProjectDto>>(projectsResult.Data.Value.GetRawText(), SerializerOptions) ?? [];

        var overall = new TaskStatusCountsDto(
            projects.Sum(p => p.TaskCounts.Todo),
            projects.Sum(p => p.TaskCounts.InProgress),
            projects.Sum(p => p.TaskCounts.Done));

        var summary = new DashboardSummaryDto(
            projects.Count,
            overall.Total,
            overall,
            projects);

        return new FnResult(true, "OK", null, JsonSerializer.SerializeToElement(summary, SerializerOptions));
    }
}
