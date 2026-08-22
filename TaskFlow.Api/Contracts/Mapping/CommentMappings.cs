using TaskFlow.Api.Domain.Entities;

namespace TaskFlow.Api.Contracts.Mapping;

public static class CommentMappings
{
    public static CommentResponse ToDto(this Comment comment) => new(
        comment.Id,
        comment.WorkItemId,
        comment.AuthorId,
        comment.Body,
        comment.CreatedAt);
}
