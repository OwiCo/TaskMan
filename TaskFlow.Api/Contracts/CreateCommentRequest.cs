namespace TaskFlow.Api.Contracts;

public sealed record CreateCommentRequest(Guid WorkItemId, Guid AuthorId, string Body);
