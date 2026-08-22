using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Application.Services;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Contracts.Mapping;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
public sealed class ProjectsController(ProjectService projectService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(request.Key, request.Name, cancellationToken);
        return Created($"/api/v1/projects/{project.Id}", project.ToDto());
    }

    [HttpGet]
    [ProducesResponseType<List<ProjectResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(cancellationToken);
        return Ok(projects.Select(p => p.ToDto()).ToList());
    }
}
