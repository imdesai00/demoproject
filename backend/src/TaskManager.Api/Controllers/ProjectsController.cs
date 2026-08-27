using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.Services;

namespace TaskManager.Api.Controllers;

public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll(CancellationToken ct)
    {
        var projects = await _projectService.GetAllForUserAsync(CurrentUserId, ct);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken ct)
    {
        var project = await _projectService.GetByIdAsync(CurrentUserId, id, ct);
        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var project = await _projectService.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectRequest request, CancellationToken ct)
    {
        var project = await _projectService.UpdateAsync(CurrentUserId, id, request, ct);
        return Ok(project);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _projectService.DeleteAsync(CurrentUserId, id, ct);
        return NoContent();
    }
}
