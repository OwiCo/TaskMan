namespace TaskFlow.Api.Contracts;

public sealed record CommentResponse(Guid Id, Guid WorkItemId, Guid AuthorId, string Body, DateTimeOffset CreatedAt);
