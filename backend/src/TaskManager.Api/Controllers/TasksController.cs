using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Services;

namespace TaskManager.Api.Controllers;

[Route("api/projects/{projectId:guid}/tasks")]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetAll(Guid projectId, CancellationToken ct)
    {
        var tasks = await _taskService.GetAllForProjectAsync(CurrentUserId, projectId, ct);
        return Ok(tasks);
    }

    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<TaskDto>> GetById(Guid projectId, Guid taskId, CancellationToken ct)
    {
        var task = await _taskService.GetByIdAsync(CurrentUserId, projectId, taskId, ct);
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(Guid projectId, CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.CreateAsync(CurrentUserId, projectId, request, ct);
        return CreatedAtAction(nameof(GetById), new { projectId, taskId = task.Id }, task);
    }

    [HttpPut("{taskId:guid}")]
    public async Task<ActionResult<TaskDto>> Update(Guid projectId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await _taskService.UpdateAsync(CurrentUserId, projectId, taskId, request, ct);
        return Ok(task);
    }

    [HttpPatch("{taskId:guid}/status")]
    public async Task<ActionResult<TaskDto>> UpdateStatus(Guid projectId, Guid taskId, UpdateTaskStatusRequest request, CancellationToken ct)
    {
        var task = await _taskService.UpdateStatusAsync(CurrentUserId, projectId, taskId, request, ct);
        return Ok(task);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid taskId, CancellationToken ct)
    {
        await _taskService.DeleteAsync(CurrentUserId, projectId, taskId, ct);
        return NoContent();
    }
}
