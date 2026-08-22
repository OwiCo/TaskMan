using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Application.Services;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Contracts.Mapping;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/workitems")]
public sealed class WorkItemsController(WorkItemService workItemService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        var workItem = await workItemService.CreateAsync(
            request.ProjectId, request.IssueType, request.Title, request.ReporterId, request.ParentId, cancellationToken);
        return Created($"/api/v1/workitems/{workItem.Id}", workItem.ToDto());
    }

    [HttpGet]
    [ProducesResponseType<List<WorkItemResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var workItems = await workItemService.GetAllAsync(cancellationToken);
        return Ok(workItems.Select(w => w.ToDto()).ToList());
    }

    [HttpPost("{id:guid}/transitions")]
    [ProducesResponseType<WorkItemResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Transition(
        Guid id, TransitionWorkItemRequest request, CancellationToken cancellationToken)
    {
        var workItem = await workItemService.TransitionAsync(id, request.To, cancellationToken);
        return Ok(workItem.ToDto());
    }
}
