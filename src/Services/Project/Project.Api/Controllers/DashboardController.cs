using Common.Web;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Services;

namespace Project.Api.Controllers;

public class DashboardController : ApiControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct) =>
        (await _dashboardService.GetSummaryAsync(CurrentActor, HttpContext.GetBearerToken(), ct)).ToActionResult();
}
