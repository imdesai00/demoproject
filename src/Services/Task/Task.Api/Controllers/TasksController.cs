using Common.Web;
using Microsoft.AspNetCore.Mvc;
using Task.Api.Models.DTOs;
using Task.Api.Services;

namespace Task.Api.Controllers;

[Route("api/projects/{projectId:guid}/tasks")]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> GetAll(Guid projectId, CancellationToken ct) =>
        (await _taskService.GetAllForProjectAsync(CurrentActor, projectId, ct)).ToActionResult();

    [HttpGet("{taskId:guid}")]
    public async System.Threading.Tasks.Task<IActionResult> GetById(Guid projectId, Guid taskId, CancellationToken ct) =>
        (await _taskService.GetByIdAsync(CurrentActor, taskId, ct)).ToActionResult();

    [HttpPost]
    public async System.Threading.Tasks.Task<IActionResult> Create(Guid projectId, CreateTaskRequestBody body, CancellationToken ct)
    {
        var request = new CreateTaskRequest(projectId, body.Title, body.Description, body.Priority, body.DueDate);
        return (await _taskService.CreateAsync(CurrentActor, request, HttpContext.GetBearerToken(), ct)).ToActionResult();
    }

    [HttpPut("{taskId:guid}")]
    public async System.Threading.Tasks.Task<IActionResult> Update(Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken ct) =>
        (await _taskService.UpdateAsync(CurrentActor, taskId, request, ct)).ToActionResult();

    [HttpPatch("{taskId:guid}/status")]
    public async System.Threading.Tasks.Task<IActionResult> UpdateStatus(Guid projectId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken ct) =>
        (await _taskService.UpdateStatusAsync(CurrentActor, taskId, request, ct)).ToActionResult();

    [HttpDelete("{taskId:guid}")]
    public async System.Threading.Tasks.Task<IActionResult> Delete(Guid projectId, Guid taskId, CancellationToken ct) =>
        (await _taskService.DeleteAsync(CurrentActor, taskId, ct)).ToActionResult();
}

/// <summary>Request body shape for POST — project_id comes from the route, not the body.</summary>
public record CreateTaskRequestBody(string Title, string? Description, string Priority, DateOnly? DueDate);
