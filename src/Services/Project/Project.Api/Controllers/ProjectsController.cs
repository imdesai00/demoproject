using Common.Web;
using Microsoft.AspNetCore.Mvc;
using Project.Api.Models.DTOs;
using Project.Api.Services;

namespace Project.Api.Controllers;

public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        (await _projectService.GetAllAsync(CurrentActor, HttpContext.GetBearerToken(), ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        (await _projectService.GetByIdAsync(CurrentActor, id, HttpContext.GetBearerToken(), ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken ct) =>
        (await _projectService.CreateAsync(CurrentActor, request, ct)).ToActionResult();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectRequest request, CancellationToken ct) =>
        (await _projectService.UpdateAsync(CurrentActor, id, request, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await _projectService.DeleteAsync(CurrentActor, id, ct)).ToActionResult();
}
