using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Application.Services;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Contracts.Mapping;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/comments")]
public sealed class CommentsController(CommentService commentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await commentService.CreateAsync(
            request.WorkItemId, request.AuthorId, request.Body, cancellationToken);
        return Created($"/api/v1/comments/{comment.Id}", comment.ToDto());
    }

    [HttpGet]
    [ProducesResponseType<List<CommentResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var comments = await commentService.GetAllAsync(cancellationToken);
        return Ok(comments.Select(c => c.ToDto()).ToList());
    }
}
