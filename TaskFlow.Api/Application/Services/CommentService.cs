using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain.Entities;
using TaskFlow.Api.Domain.Exceptions;
using TaskFlow.Api.Infrastructure.Persistence;

namespace TaskFlow.Api.Application.Services;

public sealed class CommentService(AppDbContext db, TimeProvider timeProvider)
{
    public async Task<Comment> CreateAsync(
        Guid workItemId, Guid authorId, string body, CancellationToken cancellationToken)
    {
        var workItemExists = await db.WorkItems.AnyAsync(w => w.Id == workItemId, cancellationToken);
        if (!workItemExists)
        {
            throw new NotFoundException($"Work item '{workItemId}' was not found.");
        }

        var authorExists = await db.Users.AnyAsync(u => u.Id == authorId, cancellationToken);
        if (!authorExists)
        {
            throw new NotFoundException($"User '{authorId}' was not found.");
        }

        var comment = new Comment(Guid.NewGuid(), workItemId, authorId, body, timeProvider.GetUtcNow());

        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        return comment;
    }

    public async Task<List<Comment>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Comments
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
}
