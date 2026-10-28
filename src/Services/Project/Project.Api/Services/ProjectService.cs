using System.Text.Json;
using Common.Data;
using Project.Api.Clients;
using Project.Api.Models.DTOs;
using Project.Api.Models.Entities;
using Project.Api.Repositories;

namespace Project.Api.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskServiceClient _taskServiceClient;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TaskStatusCountsDto ZeroCounts = new(0, 0, 0);

    public ProjectService(IProjectRepository projectRepository, ITaskServiceClient taskServiceClient)
    {
        _projectRepository = projectRepository;
        _taskServiceClient = taskServiceClient;
    }

    public async Task<FnResult> GetAllAsync(ActorContext actor, string bearerToken, CancellationToken ct)
    {
        var result = await _projectRepository.GetAllAsync(actor, ct);
        if (!result.Ok || result.Data is null)
        {
            return result;
        }

        var projects = result.Data.Value.EnumerateArray().Select(ProjectRepository.MapProject).ToList();
        var counts = await _taskServiceClient.GetStatusCountsAsync([.. projects.Select(p => p.Id)], bearerToken, ct);

        var dtos = projects.Select(p => ToDto(p, counts.GetValueOrDefault(p.Id, ZeroCounts))).ToList();
        return Ok(dtos);
    }

    public async Task<FnResult> GetByIdAsync(ActorContext actor, Guid projectId, string bearerToken, CancellationToken ct)
    {
        var result = await _projectRepository.GetByIdAsync(actor, projectId, ct);
        if (!result.Ok || result.Data is null)
        {
            return result;
        }

        var project = ProjectRepository.MapProject(result.Data.Value);
        var counts = await _taskServiceClient.GetStatusCountsAsync([project.Id], bearerToken, ct);

        return Ok(ToDto(project, counts.GetValueOrDefault(project.Id, ZeroCounts)));
    }

    public async Task<FnResult> CreateAsync(ActorContext actor, CreateProjectRequest request, CancellationToken ct)
    {
        var result = await _projectRepository.CreateAsync(actor, request.Name.Trim(), request.Description?.Trim(), ct);
        if (!result.Ok || result.Data is null)
        {
            return result;
        }

        return Ok(ToDto(ProjectRepository.MapProject(result.Data.Value), ZeroCounts));
    }

    public async Task<FnResult> UpdateAsync(ActorContext actor, Guid projectId, UpdateProjectRequest request, CancellationToken ct)
    {
        var result = await _projectRepository.UpdateAsync(actor, projectId, request.Name.Trim(), request.Description?.Trim(), ct);
        if (!result.Ok || result.Data is null)
        {
            return result;
        }

        // Counts are unaffected by a name/description edit — skip the extra cross-service call.
        return Ok(ToDto(ProjectRepository.MapProject(result.Data.Value), ZeroCounts));
    }

    public Task<FnResult> DeleteAsync(ActorContext actor, Guid projectId, CancellationToken ct) =>
        _projectRepository.DeleteAsync(actor, projectId, ct);

    private static ProjectDto ToDto(ProjectRecord project, TaskStatusCountsDto counts) =>
        new(project.Id, project.Name, project.Description, project.CreatedAt, project.UpdatedAt, counts);

    private static FnResult Ok(object data) => new(true, "OK", null, JsonSerializer.SerializeToElement(data, SerializerOptions));
}
