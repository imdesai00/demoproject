using Common.Web;
using Microsoft.AspNetCore.Mvc;
using Task.Api.Models.DTOs;
using Task.Api.Services;

namespace Task.Api.Controllers;

/// <summary>
/// Called only by Project.Api (ITaskServiceClient/TaskServiceClient), forwarding the original
/// end user's bearer token — so normal JWT auth + per-user ownership filtering inside the
/// Postgres function still applies. "Internal" means "no UI calls this directly", not "no auth".
/// </summary>
[Route("api/internal/tasks")]
public class InternalTasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public InternalTasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost("status-counts")]
    public async System.Threading.Tasks.Task<IActionResult> GetStatusCounts(StatusCountsRequest request, CancellationToken ct) =>
        (await _taskService.GetStatusCountsAsync(CurrentActor, request.ProjectIds, ct)).ToActionResult();
}
