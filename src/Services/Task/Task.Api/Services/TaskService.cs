using System.Text.Json;
using Common.Data;
using Task.Api.Clients;
using Task.Api.Models.DTOs;
using Task.Api.Models.Entities;
using Task.Api.Repositories;

namespace Task.Api.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectServiceClient _projectServiceClient;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public TaskService(ITaskRepository taskRepository, IProjectServiceClient projectServiceClient)
    {
        _taskRepository = taskRepository;
        _projectServiceClient = projectServiceClient;
    }

    public async System.Threading.Tasks.Task<FnResult> GetAllForProjectAsync(ActorContext actor, Guid projectId, CancellationToken ct)
    {
        var result = await _taskRepository.GetAllForProjectAsync(actor, projectId, ct);
        if (!result.Ok || result.Data is null)
        {
            return result;
        }

        var tasks = result.Data.Value.EnumerateArray().Select(TaskRepository.MapTask).Select(ToDto).ToList();
        return Ok(tasks);
    }

    public async System.Threading.Tasks.Task<FnResult> GetByIdAsync(ActorContext actor, Guid taskId, CancellationToken ct)
    {
        var result = await _taskRepository.GetByIdAsync(actor, taskId, ct);
        return result.Ok && result.Data is not null ? Ok(ToDto(TaskRepository.MapTask(result.Data.Value))) : result;
    }

    public async System.Threading.Tasks.Task<FnResult> CreateAsync(ActorContext actor, CreateTaskRequest request, string bearerToken, CancellationToken ct)
    {
        var owns = await _projectServiceClient.UserOwnsProjectAsync(request.ProjectId, bearerToken, ct);
        if (!owns)
        {
            return new FnResult(false, "FORBIDDEN", "Not your project", null);
        }

        var result = await _taskRepository.CreateAsync(
            actor, request.ProjectId, request.Title.Trim(), request.Description?.Trim(), request.Priority, request.DueDate, ct);

        return result.Ok && result.Data is not null ? Ok(ToDto(TaskRepository.MapTask(result.Data.Value))) : result;
    }

    public async System.Threading.Tasks.Task<FnResult> UpdateAsync(ActorContext actor, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var result = await _taskRepository.UpdateAsync(
            actor, taskId, request.Title.Trim(), request.Description?.Trim(), request.Priority, request.DueDate, ct);

        return result.Ok && result.Data is not null ? Ok(ToDto(TaskRepository.MapTask(result.Data.Value))) : result;
    }

    public async System.Threading.Tasks.Task<FnResult> UpdateStatusAsync(ActorContext actor, Guid taskId, UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var result = await _taskRepository.UpdateStatusAsync(actor, taskId, request.Status, ct);
        return result.Ok && result.Data is not null ? Ok(ToDto(TaskRepository.MapTask(result.Data.Value))) : result;
    }

    public System.Threading.Tasks.Task<FnResult> DeleteAsync(ActorContext actor, Guid taskId, CancellationToken ct) =>
        _taskRepository.DeleteAsync(actor, taskId, ct);

    public async System.Threading.Tasks.Task<FnResult> GetStatusCountsAsync(ActorContext actor, List<Guid> projectIds, CancellationToken ct)
    {
        var result = await _taskRepository.GetStatusCountsAsync(actor, projectIds, ct);
        if (!result.Ok || result.Data is null)
        {
            return result;
        }

        var entries = result.Data.Value.EnumerateArray().Select(e => new StatusCountsEntry(
            e.GetGuid("project_id"), e.GetInt("todo"), e.GetInt("in_progress"), e.GetInt("done"))).ToList();

        return Ok(entries);
    }

    private static TaskDto ToDto(TaskRecord task) => new(
        task.Id, task.ProjectId, task.Title, task.Description, task.Status, task.Priority,
        task.DueDate, task.CreatedAt, task.UpdatedAt);

    private static FnResult Ok(object data) => new(true, "OK", null, JsonSerializer.SerializeToElement(data, SerializerOptions));
}
