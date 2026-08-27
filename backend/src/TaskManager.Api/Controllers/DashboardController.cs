using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Dashboard;
using TaskManager.Application.Services;

namespace TaskManager.Api.Controllers;

public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
    {
        var summary = await _dashboardService.GetSummaryAsync(CurrentUserId, ct);
        return Ok(summary);
    }
}
