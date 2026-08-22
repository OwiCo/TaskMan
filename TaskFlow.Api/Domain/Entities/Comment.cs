namespace TaskFlow.Api.Domain.Entities;

public sealed class Comment
{
    public Guid Id { get; private set; }
    public Guid WorkItemId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Comment(Guid id, Guid workItemId, Guid authorId, string body, DateTimeOffset createdAt)
    {
        if (workItemId == Guid.Empty)
        {
            throw new ArgumentException("Comment must belong to a work item.", nameof(workItemId));
        }

        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("Comment must have an author.", nameof(authorId));
        }

        if (string.IsNullOrWhiteSpace(body) || body.Length > 2000)
        {
            throw new ArgumentException(
                "Comment body must be non-blank and at most 2000 characters.", nameof(body));
        }

        Id = id;
        WorkItemId = workItemId;
        AuthorId = authorId;
        Body = body;
        CreatedAt = createdAt;
    }

#pragma warning disable CS8618 // required for future EF Core materialization; all properties set via reflection.
    private Comment()
    {
    }
#pragma warning restore CS8618
}
